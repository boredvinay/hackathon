using DesignModule.Services.DTO;

namespace DesignModule.Services.Interfaces;

public interface IDesignService
{
    Task<PagedResult<DesignListItem>> ListDesignsAsync(ListDesignsQuery q, CancellationToken ct);

    Task<Guid> CreateDesignAsync(CreateDesignRequest req, CancellationToken ct);
    Task<DesignDto> GetDesignAsync(Guid designId, CancellationToken ct);
    Task<IReadOnlyList<DesignVersionListItem>> ListPublishedAsync(Guid? designId, CancellationToken ct);

    Task<Guid> CreateVersionAsync(Guid designId, CreateDesignVersionRequest req, CancellationToken ct);
    Task<DesignVersionDto> GetVersionAsync(Guid versionId, CancellationToken ct);
    Task<string> GetDslAsync(Guid versionId, CancellationToken ct);
    Task SaveDslAsync(Guid versionId, SaveDslRequest req, CancellationToken ct);

    Task<string> GetSchemaAsync(Guid versionId, CancellationToken ct);
    Task SaveSchemaAsync(Guid versionId, SaveSchemaRequest req, CancellationToken ct);

    Task SubmitAsync(Guid versionId, CancellationToken ct);
    Task ApproveAsync(Guid versionId, ApproveRequest req, CancellationToken ct);

    Task<PreviewResponse> GeneratePreviewAsync(Guid versionId, CancellationToken ct);
}