using RenderModule.Services.DTO;

namespace RenderModule.Services.Interfaces;

public interface IDiffService
{
    Task<DiffResponse> DiffAsync(DiffRequest req, CancellationToken ct);
}