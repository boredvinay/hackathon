namespace RenderModule.Services.DTO;

public sealed class RenderBatchRequest
{
    public Guid DesignVersionId { get; set; }
    public List<RenderBatchItem> Items { get; set; } = new();
    public string Format { get; set; } = "pdf";
    public string Bundle { get; set; } = "zip"; // or "pdf-merge" (phase-2)
}

public sealed class RenderBatchItem
{
    public string Id { get; set; } = "";
    public Dictionary<string, object> Payload { get; set; } = new();
}