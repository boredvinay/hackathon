public sealed class JobRow
{
    public Guid Id { get; init; }
    public Guid TemplateVersionId { get; init; }
    public string Type { get; init; } = "Bulk";
    public string Status { get; set; } = "Queued";
    public string? IdempotencyKey { get; init; }
    public string? WebhookUrl { get; init; }
    public string? ResultPath { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
}