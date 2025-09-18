using System.Data.Common;
using Microsoft.Data.SqlClient;
using SharedModule;

namespace JobsModule;

public sealed class SqlJobProvider(IDbConnectionFactory factory) : IJobProvider
{
    public async Task<Guid?> FindByIdempotencyKeyAsync(string key, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Id FROM Jobs WHERE IdempotencyKey=@k";
        cmd.Parameters.Add(new SqlParameter("@k", key));

        var r = await cmd.ExecuteScalarAsync(ct);
        return r is Guid g ? g : null;
    }

    public async Task CreateJobAsync(
        Guid id,
        Guid templateVersionId,
        string type,
        string status,
        IEnumerable<string> itemsJson,
        string? idempotencyKey,
        string? webhookUrl,
        CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        // ✅ FK guard: ensure TemplateVersions.Id exists BEFORE we insert
        var exists = await TemplateVersionExistsAsync(c, templateVersionId, ct);
        if (!exists)
            throw new InvalidOperationException($"TemplateVersionId '{templateVersionId}' does not exist.");

        await using var tx = await c.BeginTransactionAsync(ct);
        await InsertJob(c, tx, id, templateVersionId, type, status, idempotencyKey, webhookUrl, ct);
        foreach (var payload in itemsJson)
            await InsertItem(c, tx, id, payload, ct);
        await tx.CommitAsync(ct);
    }

    private static async Task<bool> TemplateVersionExistsAsync(DbConnection c, Guid templateVersionId, CancellationToken ct)
    {
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM TemplateVersions WHERE Id=@Id";
        cmd.Parameters.Add(new SqlParameter("@Id", templateVersionId));
        var r = await cmd.ExecuteScalarAsync(ct);
        return r is not null && r != DBNull.Value;
    }

    private static async Task InsertJob(
        DbConnection c,
        DbTransaction tx,
        Guid id,
        Guid templateVersionId,
        string type,
        string status,
        string? idempotencyKey,
        string? webhookUrl,
        CancellationToken ct)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
          INSERT INTO Jobs (Id, Type, TemplateVersionId, Status, IdempotencyKey, WebhookUrl, ResultPath, Error, CreatedAt, CompletedAt)
          VALUES (@Id, @Type, @TplVerId, @Status, @Idem, @Webhook, NULL, NULL, SYSUTCDATETIME(), NULL)
        """;
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        cmd.Parameters.Add(new SqlParameter("@Type", type));
        cmd.Parameters.Add(new SqlParameter("@TplVerId", templateVersionId));
        cmd.Parameters.Add(new SqlParameter("@Status", status));
        cmd.Parameters.Add(new SqlParameter("@Idem", (object?)idempotencyKey ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@Webhook", (object?)webhookUrl ?? DBNull.Value));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertItem(DbConnection c, DbTransaction tx, Guid jobId, string payloadJson, CancellationToken ct)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO JobItems (Id, JobId, PayloadJson, Status, Error) VALUES (@Id,@JobId,@Payload,'Queued',NULL)";
        cmd.Parameters.Add(new SqlParameter("@Id", Guid.NewGuid()));
        cmd.Parameters.Add(new SqlParameter("@JobId", jobId));
        cmd.Parameters.Add(new SqlParameter("@Payload", payloadJson));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<JobRow?> GetJobAsync(Guid id, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT Id, TemplateVersionId, Type, Status, IdempotencyKey, WebhookUrl, ResultPath, Error, CreatedAt, CompletedAt
            FROM Jobs WHERE Id=@Id
        """;
        cmd.Parameters.Add(new SqlParameter("@Id", id));

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new JobRow
        {
            Id = r.GetGuid(0),
            TemplateVersionId = r.GetGuid(1),
            Type = r.GetString(2),
            Status = r.GetString(3),
            IdempotencyKey = r.IsDBNull(4) ? null : r.GetString(4),
            WebhookUrl = r.IsDBNull(5) ? null : r.GetString(5),
            ResultPath = r.IsDBNull(6) ? null : r.GetString(6),
            Error = r.IsDBNull(7) ? null : r.GetString(7),
            CreatedAt = r.GetDateTime(8),
            CompletedAt = r.IsDBNull(9) ? null : r.GetDateTime(9)
        };
    }

    public async Task<(int total, int completed, int failed)> GetJobProgressAsync(Guid id, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
          SELECT COUNT(*) AS Total,
                 SUM(CASE WHEN Status='Completed' THEN 1 ELSE 0 END) AS Completed,
                 SUM(CASE WHEN Status='Failed' THEN 1 ELSE 0 END) AS Failed
          FROM JobItems WHERE JobId=@Id
        """;
        cmd.Parameters.Add(new SqlParameter("@Id", id));

        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return (r.GetInt32(0), r.IsDBNull(1) ? 0 : r.GetInt32(1), r.IsDBNull(2) ? 0 : r.GetInt32(2));
    }

    public async Task UpdateJobStatusAsync(Guid id, string status, string? resultPath, string? error, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
          UPDATE Jobs
             SET Status=@s,
                 ResultPath=@r,
                 Error=@e,
                 CompletedAt = CASE WHEN @s IN ('Completed','Failed','Cancelled') THEN SYSUTCDATETIME() ELSE CompletedAt END
           WHERE Id=@id
        """;
        cmd.Parameters.Add(new SqlParameter("@s", status));
        cmd.Parameters.Add(new SqlParameter("@r", (object?)resultPath ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@e", (object?)error ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@id", id));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<(Guid itemId, string payloadJson)>> GetPendingItemsAsync(Guid jobId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id, PayloadJson FROM JobItems WHERE JobId=@Id AND Status='Queued'";
        cmd.Parameters.Add(new SqlParameter("@Id", jobId));

        var list = new List<(Guid, string)>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add((r.GetGuid(0), r.GetString(1)));
        return list;
    }

    public Task MarkItemCompletedAsync(Guid itemId, CancellationToken ct)
        => SetItem(itemId, "Completed", null, ct);

    public Task MarkItemFailedAsync(Guid itemId, string error, CancellationToken ct)
        => SetItem(itemId, "Failed", error, ct);

    private async Task SetItem(Guid itemId, string status, string? error, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE JobItems SET Status=@s, Error=@e WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@s", status));
        cmd.Parameters.Add(new SqlParameter("@e", (object?)error ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@id", itemId));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
