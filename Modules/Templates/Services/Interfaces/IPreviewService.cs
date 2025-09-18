namespace DesignModule.Services.Interfaces;

public interface IPreviewService
{
    Task<string> RenderPreviewAsync(Guid versionId, string dslJson, CancellationToken ct);
}