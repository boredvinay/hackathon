namespace RenderModule.Services.DTO;

public sealed class RenderJobStatusResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = "Completed"; // Queued|Running|Completed|Failed
    public string? ResultUrl { get; set; }
    public List<RenderBatchItemStatus> Items { get; set; } = new();
}

public sealed class RenderBatchItemStatus
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "Completed";
    public string? Error { get; set; }
}