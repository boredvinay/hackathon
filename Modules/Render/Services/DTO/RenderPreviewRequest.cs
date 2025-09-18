namespace RenderModule.Services.DTO;

public sealed class RenderPreviewRequest
{
    public Guid DesignVersionId { get; set; }
    public Dictionary<string, object>? Payload { get; set; } // optional sample data
    public string Format { get; set; } = "pdf";               // "pdf" | "png"
    public int? Dpi { get; set; }                              // optional override
    public PageSpec? Page { get; set; }                        // optional override
}

public sealed class PageSpec { public int Width { get; set; } public int Height { get; set; } }