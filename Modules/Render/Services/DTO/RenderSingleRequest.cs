namespace RenderModule.Services.DTO;

public sealed class RenderSingleRequest
{
    public Guid DesignVersionId { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
    public string Format { get; set; } = "pdf";
}