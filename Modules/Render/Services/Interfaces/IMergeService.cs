using RenderModule.Services.DTO;

namespace RenderModule.Services.Interfaces;

public interface IMergeService
{
    Task<RenderResultResponse> MergeAsync(MergePdfRequest req, CancellationToken ct);
}