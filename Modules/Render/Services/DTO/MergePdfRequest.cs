namespace RenderModule.Services.DTO;

public sealed class MergePdfRequest
{
    // Accept either server fileIds (from earlier renders) or raw base64 PDFs.
    public List<string>? FileIds { get; set; }
    public List<string>? PdfBase64 { get; set; }
    public string OutputName { get; set; } = "merged.pdf";
}