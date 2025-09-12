namespace Persistence.Models;

public enum JobItemStatus { Queued, Running, Completed, Failed, Skipped }

public sealed class JobItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    public string PayloadJson { get; set; } = "{}";
    public JobItemStatus Status { get; set; } = JobItemStatus.Queued;
    public string? Error { get; set; }
}