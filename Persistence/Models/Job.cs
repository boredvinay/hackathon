namespace Persistence.Models;

public enum JobType { Single, Bulk }
public enum JobStatus { Queued, Running, Completed, Failed, Cancelled }

public sealed class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public JobType Type { get; set; } = JobType.Single;
    public JobStatus Status { get; set; } = JobStatus.Queued;

    public string? IdempotencyKey { get; set; }
    public string? TemplateKey { get; set; }        // optional for now
    public string? TemplateVersion { get; set; }    // optional for now

    public string? ResultPath { get; set; }
    public string? Error { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<JobItem> Items { get; set; } = new List<JobItem>();
}