namespace RenderModule.Services.DTO;

public sealed class RenderResultResponse
{
    public Guid RenderId { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public string Url { get; set; } = "";
}