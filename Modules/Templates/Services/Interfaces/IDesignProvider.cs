using DesignModule.Services.DTO;

namespace DesignModule.Services.Interfaces;

public interface IDesignProvider
{
    // NEW: list designs (paged)
    Task<(IReadOnlyList<DesignListItem> items, int total)> ListDesignsAsync(string? q, string? status, int page, int pageSize, CancellationToken ct);

    // Designs
    Task InsertDesignAsync(Guid id, string key, string status, string createdBy, CancellationToken ct);
    Task<DesignDto?> GetDesignAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<DesignVersionListItem>> ListPublishedAsync(Guid? designId, CancellationToken ct);

    // Versions
    Task InsertDesignVersionAsync(Guid versionId, Guid designId, string semVer, string state,
        string dslJson, string? previewPath, string sha256,
        string createdBy, CancellationToken ct);

    Task<DesignVersionDto?> GetVersionAsync(Guid versionId, CancellationToken ct);

    Task<string?> GetDslAsync(Guid versionId, CancellationToken ct);
    Task UpdateDslAsync(Guid versionId, string dslJson, string sha256, CancellationToken ct);

    Task<string?> GetSchemaAsync(Guid versionId, CancellationToken ct);
    Task UpsertSchemaAsync(Guid versionId, string schemaJson, CancellationToken ct);

    Task UpdateVersionStateAsync(Guid versionId, string newState, CancellationToken ct);
    Task InsertApprovalAsync(Guid id, Guid versionId, string reviewer, string signatureHash, DateTime timestampUtc, CancellationToken ct);

    Task UpdatePreviewPathAsync(Guid versionId, string previewPath, CancellationToken ct);
}