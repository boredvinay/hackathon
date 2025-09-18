using System.Data.Common;
using Microsoft.Data.SqlClient;
using SharedModule;
using DesignModule.Services.DTO;
using DesignModule.Services.Interfaces;

namespace DesignModule.Providers;

public sealed class SqlDesignProvider(IDbConnectionFactory factory) : IDesignProvider
{
    // -------- NEW: list designs (paged) --------
    public async Task<(IReadOnlyList<DesignListItem> items, int total)> ListDesignsAsync(string? q, string? status, int page, int pageSize, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        var where = "WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(q)) where += " AND d.[Key] LIKE @Q";
        if (!string.IsNullOrWhiteSpace(status)) where += " AND d.[Status] = @Status";

        var sqlPage = """
            SELECT d.Id, d.[Key], d.[Status], d.CreatedAt, d.CreatedBy,
                   (SELECT TOP 1 v.Id FROM DesignVersions v WHERE v.DesignId = d.Id ORDER BY v.CreatedAt DESC) AS LatestVersionId
            FROM Designs d
        """ + " " + where + " ORDER BY d.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;";

        var sqlCount = "SELECT COUNT(1) FROM Designs d " + " " + where + ";";

        int skip = (page - 1) * pageSize;

        var items = new List<DesignListItem>();

        await using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = sqlPage;
            if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.Add(new SqlParameter("@Q", $"%{q}%"));
            if (!string.IsNullOrWhiteSpace(status)) cmd.Parameters.Add(new SqlParameter("@Status", status));
            cmd.Parameters.Add(new SqlParameter("@Skip", skip));
            cmd.Parameters.Add(new SqlParameter("@Take", pageSize));

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                items.Add(new DesignListItem
                {
                    Id = r.GetGuid(0),
                    Key = r.GetString(1),
                    Status = r.GetString(2),
                    CreatedAt = r.GetDateTime(3),
                    CreatedBy = r.GetString(4),
                    LatestVersionId = await r.IsDBNullAsync(5, ct) ? null : r.GetGuid(5)
                });
            }
        }

        int total;
        await using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = sqlCount;
            if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.Add(new SqlParameter("@Q", $"%{q}%"));
            if (!string.IsNullOrWhiteSpace(status)) cmd.Parameters.Add(new SqlParameter("@Status", status));
            total = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        return (items, total);
    }

    // ---------------- Designs ----------------

    public async Task InsertDesignAsync(Guid id, string key, string status, string createdBy, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
          INSERT INTO Designs (Id, [Key], [Status], CreatedAt, CreatedBy)
          VALUES (@Id, @Key, @Status, SYSUTCDATETIME(), @By)
        """;
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        cmd.Parameters.Add(new SqlParameter("@Key", key));
        cmd.Parameters.Add(new SqlParameter("@Status", status));
        cmd.Parameters.Add(new SqlParameter("@By", createdBy));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<DesignDto?> GetDesignAsync(Guid id, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        var sql = """
          SELECT d.Id, d.[Key], d.[Status], d.CreatedAt, d.CreatedBy,
                 (SELECT TOP 1 v.Id FROM DesignVersions v WHERE v.DesignId = d.Id ORDER BY v.CreatedAt DESC) AS LatestVersionId
          FROM Designs d WHERE d.Id = @Id
        """;

        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Id", id));

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new DesignDto
        {
            Id = r.GetGuid(0),
            Key = r.GetString(1),
            Status = r.GetString(2),
            CreatedAt = r.GetDateTime(3),
            CreatedBy = r.GetString(4),
            LatestVersionId = await r.IsDBNullAsync(5, ct) ? null : r.GetGuid(5)
        };
    }

    public async Task<IReadOnlyList<DesignVersionListItem>> ListPublishedAsync(Guid? designId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        var sql = designId is null
            ? """
               SELECT Id, DesignId, SemVer, [State], PreviewPath, CreatedAt
               FROM DesignVersions
               WHERE [State] = 'Published'
               ORDER BY CreatedAt DESC
              """
            : """
               SELECT Id, DesignId, SemVer, [State], PreviewPath, CreatedAt
               FROM DesignVersions
               WHERE [State] = 'Published' AND DesignId=@D
               ORDER BY CreatedAt DESC
              """;

        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        if (designId is not null) cmd.Parameters.Add(new SqlParameter("@D", designId));

        var list = new List<DesignVersionListItem>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new DesignVersionListItem
            {
                Id = r.GetGuid(0),
                DesignId = r.GetGuid(1),
                SemVer = r.GetString(2),
                State = r.GetString(3),
                PreviewPath = r.IsDBNull(4) ? null : r.GetString(4),
                CreatedAt = r.GetDateTime(5)
            });
        }
        return list;
    }

    // ---------------- Versions ----------------

    public async Task InsertDesignVersionAsync(Guid versionId, Guid designId, string semVer, string state,
                                               string dslJson, string? previewPath, string sha256,
                                               string createdBy, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);

        await using (var check = c.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT 1 FROM Designs WHERE Id=@Id";
            check.Parameters.Add(new SqlParameter("@Id", designId));
            var exists = await check.ExecuteScalarAsync(ct);
            if (exists is null) throw new InvalidOperationException($"Design {designId} not found.");
        }

        await using (var cmd = c.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
              INSERT INTO DesignVersions (Id, DesignId, SemVer, [State], JsonDsl, PreviewPath, Sha256, CreatedAt, CreatedBy)
              VALUES (@Id, @DesignId, @SemVer, @State, @Dsl, @Preview, @Sha, SYSUTCDATETIME(), @By)
            """;
            cmd.Parameters.Add(new SqlParameter("@Id", versionId));
            cmd.Parameters.Add(new SqlParameter("@DesignId", designId));
            cmd.Parameters.Add(new SqlParameter("@SemVer", semVer));
            cmd.Parameters.Add(new SqlParameter("@State", state));
            cmd.Parameters.Add(new SqlParameter("@Dsl", dslJson));
            cmd.Parameters.Add(new SqlParameter("@Preview", (object?)previewPath ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Sha", sha256));
            cmd.Parameters.Add(new SqlParameter("@By", createdBy));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<DesignVersionDto?> GetVersionAsync(Guid versionId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
          SELECT Id, DesignId, SemVer, [State], Sha256, PreviewPath, CreatedAt, CreatedBy
          FROM DesignVersions WHERE Id=@Id
        """;
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new DesignVersionDto
        {
            Id = r.GetGuid(0),
            DesignId = r.GetGuid(1),
            SemVer = r.GetString(2),
            State = r.GetString(3),
            Sha256 = r.GetString(4),
            PreviewPath = r.IsDBNull(5) ? null : r.GetString(5),
            CreatedAt = r.GetDateTime(6),
            CreatedBy = r.GetString(7)
        };
    }

    public async Task<string?> GetDslAsync(Guid versionId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT JsonDsl FROM DesignVersions WHERE Id=@Id";
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));
        var r = await cmd.ExecuteScalarAsync(ct);
        return r as string;
    }

    public async Task UpdateDslAsync(Guid versionId, string dslJson, string sha256, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
          UPDATE DesignVersions SET JsonDsl=@Dsl, Sha256=@Sha
          WHERE Id=@Id
        """;
        cmd.Parameters.Add(new SqlParameter("@Dsl", dslJson));
        cmd.Parameters.Add(new SqlParameter("@Sha", sha256));
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<string?> GetSchemaAsync(Guid versionId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT JsonSchema FROM Schemas WHERE DesignVersionId=@V";
        cmd.Parameters.Add(new SqlParameter("@V", versionId));
        var r = await cmd.ExecuteScalarAsync(ct);
        return r as string;
    }

    public async Task UpsertSchemaAsync(Guid versionId, string schemaJson, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);

        var rows = 0;
        await using (var up = c.CreateCommand())
        {
            up.CommandText = "UPDATE Schemas SET JsonSchema=@S WHERE DesignVersionId=@V";
            up.Parameters.Add(new SqlParameter("@S", schemaJson));
            up.Parameters.Add(new SqlParameter("@V", versionId));
            rows = await up.ExecuteNonQueryAsync(ct);
        }
        if (rows == 0)
        {
            await using var ins = c.CreateCommand();
            ins.CommandText = """
              INSERT INTO Schemas (Id, DesignVersionId, JsonSchema)
              VALUES (@Id, @V, @S)
            """;
            ins.Parameters.Add(new SqlParameter("@Id", Guid.NewGuid()));
            ins.Parameters.Add(new SqlParameter("@V", versionId));
            ins.Parameters.Add(new SqlParameter("@S", schemaJson));
            await ins.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task UpdateVersionStateAsync(Guid versionId, string newState, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE DesignVersions SET [State]=@S WHERE Id=@Id";
        cmd.Parameters.Add(new SqlParameter("@S", newState));
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task InsertApprovalAsync(Guid id, Guid versionId, string reviewer, string signatureHash, DateTime timestampUtc, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);

        await using (var cmd = c.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
              INSERT INTO Approvals (Id, DesignVersionId, Reviewer, SignatureHash, [Timestamp])
              VALUES (@Id, @V, @R, @H, @Ts)
            """;
            cmd.Parameters.Add(new SqlParameter("@Id", id));
            cmd.Parameters.Add(new SqlParameter("@V", versionId));
            cmd.Parameters.Add(new SqlParameter("@R", reviewer));
            cmd.Parameters.Add(new SqlParameter("@H", signatureHash));
            cmd.Parameters.Add(new SqlParameter("@Ts", timestampUtc));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task UpdatePreviewPathAsync(Guid versionId, string previewPath, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE DesignVersions SET PreviewPath=@P WHERE Id=@Id";
        cmd.Parameters.Add(new SqlParameter("@P", previewPath));
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
