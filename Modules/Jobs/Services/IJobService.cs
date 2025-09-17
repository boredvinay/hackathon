using JobsModule.Services.DTO;

namespace JobsModule.Services;

public interface IJobService
{
    Task<Guid> CreateAsync(CreateJobRequest req, string? idempotencyKey, CancellationToken ct);
    Task<JobStatusResponse> GetStatusAsync(Guid id, CancellationToken ct);
    Task<(Stream? stream, string fileName)> GetResultAsync(Guid id, CancellationToken ct);
    Task<bool> CancelAsync(Guid id, CancellationToken ct);
}