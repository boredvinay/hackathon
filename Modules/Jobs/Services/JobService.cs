using System.Text.Json;
using JobsModule.Dependencies.Interfaces;
using JobsModule.Services.DTO;

namespace JobsModule.Services;

public sealed class JobService(IJobProvider db, IJobQueue queue) : IJobService
{
    public async Task<Guid> CreateAsync(CreateJobRequest req, string? idem, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(idem))
        {
            var existing = await db.FindByIdempotencyKeyAsync(idem!, ct);
            if (existing is Guid id0) return id0;
        }

        var id = Guid.NewGuid();
        var type = !string.IsNullOrWhiteSpace(req.Type)
            ? req.Type!
            : (req.Items.Count <= 1 ? "Single" : "Bulk");

        var itemsJson = req.Items.Select(i => JsonSerializer.Serialize(i));

        await db.CreateJobAsync(
            id: id,
            templateVersionId: req.EffectiveDesignVersionId, // <-- maps to DB column TemplateVersionId
            type: type,
            status: "Queued",
            itemsJson: itemsJson,
            idempotencyKey: idem,
            webhookUrl: req.WebhookUrl,
            ct: ct);

        await queue.EnqueueAsync(new QueuedJob(id), ct);
        return id;
    }

    public async Task<JobStatusResponse> GetStatusAsync(Guid id, CancellationToken ct)
    {
        var job = await db.GetJobAsync(id, ct) ?? throw new KeyNotFoundException($"Job {id} not found");
        var (total, completed, failed) = await db.GetJobProgressAsync(id, ct);

        return new JobStatusResponse
        {
            Id = id,
            Status = job.Status,
            Total = total,
            Completed = completed,
            Failed = failed,
            ResultUrl = job.Status == "Completed" && job.ResultPath is not null
                ? $"/api/jobs/{id}/result"
                : null
        };
    }

    public async Task<(Stream?, string)> GetResultAsync(Guid id, CancellationToken ct)
    {
        var job = await db.GetJobAsync(id, ct);
        if (job?.ResultPath is null || !File.Exists(job.ResultPath)) return (null, "");
        return (File.OpenRead(job.ResultPath), Path.GetFileName(job.ResultPath));
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct)
    {
        var job = await db.GetJobAsync(id, ct);
        if (job is null) return false;
        if (job.Status is "Completed" or "Failed" or "Cancelled") return false;

        await db.UpdateJobStatusAsync(id, "Cancelled", null, null, ct);
        return true;
    }
}
