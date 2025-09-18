using System.Security.Cryptography;
using System.Text;
using DesignModule.Services.DTO;
using DesignModule.Services.Interfaces;

namespace DesignModule.Services;

public sealed class DesignService(IDesignProvider db, IPreviewService preview) : IDesignService
{
    // -------- NEW: list designs (paged) --------
    public async Task<PagedResult<DesignListItem>> ListDesignsAsync(ListDesignsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var size = q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 200);
        var (items, total) = await db.ListDesignsAsync(q.Q, q.Status, page, size, ct);
        return new PagedResult<DesignListItem>
        {
            Items = items,
            Page = page,
            PageSize = size,
            Total = total
        };
    }

    public async Task<Guid> CreateDesignAsync(CreateDesignRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Key))
            throw new ArgumentException("Key is required.", nameof(req.Key));

        var id = Guid.NewGuid();
        await db.InsertDesignAsync(id, req.Key.Trim(), "Active", req.CreatedBy, ct);
        return id;
    }

    public Task<DesignDto> GetDesignAsync(Guid designId, CancellationToken ct)
        => db.GetDesignAsync(designId, ct) ?? throw new KeyNotFoundException($"Design {designId} not found");

    public Task<IReadOnlyList<DesignVersionListItem>> ListPublishedAsync(Guid? designId, CancellationToken ct)
        => db.ListPublishedAsync(designId, ct);

    public async Task<Guid> CreateVersionAsync(Guid designId, CreateDesignVersionRequest req, CancellationToken ct)
    {
        var versionId = Guid.NewGuid();
        var dsl = req.DslJson ?? MinimalDslJson();
        var sha = Sha256(dsl);
        await db.InsertDesignVersionAsync(versionId, designId, req.SemVer, "Draft", dsl, previewPath: null, sha, req.CreatedBy, ct);

        if (req.SchemaJson is not null)
            await db.UpsertSchemaAsync(versionId, req.SchemaJson, ct);

        return versionId;
    }

    public async Task<DesignVersionDto> GetVersionAsync(Guid versionId, CancellationToken ct)
        => await db.GetVersionAsync(versionId, ct) ?? throw new KeyNotFoundException($"Version {versionId} not found");

    public async Task<string> GetDslAsync(Guid versionId, CancellationToken ct)
        => await db.GetDslAsync(versionId, ct) ?? throw new KeyNotFoundException($"DSL for {versionId} not found");

    public async Task SaveDslAsync(Guid versionId, SaveDslRequest req, CancellationToken ct)
    {
        var v = await db.GetVersionAsync(versionId, ct) ?? throw new KeyNotFoundException($"Version {versionId} not found");
        if (!string.Equals(v.State, "Draft", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Version {versionId} is not Draft.");

        var sha = Sha256(req.DslJson);
        await db.UpdateDslAsync(versionId, req.DslJson, sha, ct);
    }

    public async Task<string> GetSchemaAsync(Guid versionId, CancellationToken ct)
        => await db.GetSchemaAsync(versionId, ct) ?? throw new KeyNotFoundException($"Schema for {versionId} not found");

    public async Task SaveSchemaAsync(Guid versionId, SaveSchemaRequest req, CancellationToken ct)
    {
        var v = await db.GetVersionAsync(versionId, ct) ?? throw new KeyNotFoundException($"Version {versionId} not found");
        if (!string.Equals(v.State, "Draft", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Version {versionId} is not Draft.");

        await db.UpsertSchemaAsync(versionId, req.SchemaJson, ct);
    }

    public async Task SubmitAsync(Guid versionId, CancellationToken ct)
    {
        var v = await db.GetVersionAsync(versionId, ct) ?? throw new KeyNotFoundException($"Version {versionId} not found");
        if (!string.Equals(v.State, "Draft", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Only Draft can be submitted.");
        await db.UpdateVersionStateAsync(versionId, "InReview", ct);
    }

    public async Task ApproveAsync(Guid versionId, ApproveRequest req, CancellationToken ct)
    {
        var v = await db.GetVersionAsync(versionId, ct) ?? throw new KeyNotFoundException($"Version {versionId} not found");
        if (v.State is not ("Draft" or "InReview"))
            throw new InvalidOperationException("Only Draft/InReview can be approved.");

        var dsl = await db.GetDslAsync(versionId, ct) ?? throw new InvalidOperationException("DSL missing.");
        _ = await db.GetSchemaAsync(versionId, ct) ?? throw new InvalidOperationException("Schema missing.");

        await db.InsertApprovalAsync(Guid.NewGuid(), versionId, req.Reviewer, req.SignatureHash, DateTime.UtcNow, ct);
        await db.UpdateVersionStateAsync(versionId, "Published", ct);
    }

    public async Task<PreviewResponse> GeneratePreviewAsync(Guid versionId, CancellationToken ct)
    {
        var dsl = await db.GetDslAsync(versionId, ct) ?? throw new KeyNotFoundException($"DSL for {versionId} not found");
        var path = await preview.RenderPreviewAsync(versionId, dsl, ct);
        await db.UpdatePreviewPathAsync(versionId, path, ct);
        return new PreviewResponse { VersionId = versionId, PreviewPath = path };
    }

    // ------------------ helpers ------------------

    private static string Sha256(string s)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string MinimalDslJson()
    {
        return """
        {
          "version": "1.0",
          "design": { "name": "NewDesign", "units": "px", "dpi": 203, "size": { "width": 800, "height": 600 } },
          "widgets": []
        }
        """;
    }
}
