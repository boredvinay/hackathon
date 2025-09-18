namespace JobsModule.Services.DTO;

public sealed class JobStatusResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Queued";
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public string? ResultUrl { get; set; }
}