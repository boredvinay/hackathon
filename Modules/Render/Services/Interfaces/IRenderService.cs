using RenderModule.Services.DTO;

namespace RenderModule.Services.Interfaces;

public interface IRenderService
{
    Task<RenderResultResponse> PreviewAsync(RenderPreviewRequest req, CancellationToken ct);
    Task<RenderResultResponse> RenderSingleAsync(RenderSingleRequest req, string? idemKey, CancellationToken ct);
    Task<RenderJobStatusResponse> RenderBatchAsync(RenderBatchRequest req, CancellationToken ct);
    Task<RenderJobStatusResponse> GetBatchStatusAsync(Guid jobId, CancellationToken ct);

    Task<(string path, string? contentType)> ResolveFileAsync(string fileId, CancellationToken ct);
}