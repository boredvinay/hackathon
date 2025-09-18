using RenderModule.Services.DTO;

namespace RenderModule.Providers;

public interface IDesignReadProvider
{
    Task<string?> GetDslAsync(Guid versionId, CancellationToken ct);
    Task<string?> GetSchemaAsync(Guid versionId, CancellationToken ct);
    Task<DesignVersionHead?> GetVersionAsync(Guid versionId, CancellationToken ct);
}