using Microsoft.Data.SqlClient;
using RenderModule.Services.DTO;

namespace RenderModule.Providers;

public sealed class DesignReadProvider(IDbConnectionFactory factory) : IDesignReadProvider
{
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

    public async Task<string?> GetSchemaAsync(Guid versionId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT JsonSchema FROM Schemas WHERE DesignVersionId=@Id";
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));
        var r = await cmd.ExecuteScalarAsync(ct);
        return r as string;
    }

    public async Task<DesignVersionHead?> GetVersionAsync(Guid versionId, CancellationToken ct)
    {
        await using var c = factory.Create();
        await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
                              SELECT Id, DesignId, SemVer, [State], PreviewPath
                              FROM DesignVersions WHERE Id=@Id
                          """;
        cmd.Parameters.Add(new SqlParameter("@Id", versionId));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new DesignVersionHead
        {
            Id = r.GetGuid(0),
            DesignId = r.GetGuid(1),
            SemVer = r.GetString(2),
            State = r.GetString(3),
            PreviewPath = r.IsDBNull(4) ? null : r.GetString(4)
        };
    }
}