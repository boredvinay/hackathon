using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using RenderModule.Services.DTO;
using RenderModule.Services.Interfaces;
using SharedModule;

namespace RenderModule.Services;

public sealed class MergeService(IFileStore files) : IMergeService
{
    public async Task<RenderResultResponse> MergeAsync(MergePdfRequest req, CancellationToken ct)
    {
        if ((req.FileIds is null || req.FileIds.Count == 0) && (req.PdfBase64 is null || req.PdfBase64.Count == 0))
            throw new ArgumentException("Provide at least one fileId or base64 PDF.");

        var outId = Guid.NewGuid();
        var outPath = files.Combine("renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), $"{outId}.pdf");
        await files.CreateDirectoryAsync(Path.GetDirectoryName(outPath)!);

        using var output = new PdfDocument();

        // Add pages from fileIds
        if (req.FileIds is not null)
        {
            foreach (var fid in req.FileIds)
            {
                var (path, _) = await ResolveFromTodayAsync(files, fid);
                if (!File.Exists(path)) continue;

                using var input = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                for (int i = 0; i < input.PageCount; i++)
                    output.AddPage(input.Pages[i]);
            }
        }

        // Add pages from base64 PDFs
        if (req.PdfBase64 is not null)
        {
            foreach (var b64 in req.PdfBase64)
            {
                var bytes = Convert.FromBase64String(b64);
                using var ms = new MemoryStream(bytes);
                using var input = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                for (int i = 0; i < input.PageCount; i++)
                    output.AddPage(input.Pages[i]);
            }
        }

        output.Save(outPath);

        return new RenderResultResponse
        {
            RenderId = outId,
            ContentType = "application/pdf",
            Url = $"/api/render/files/{outId}.pdf"
        };
    }

    private static Task<(string path, string? contentType)> ResolveFromTodayAsync(IFileStore files, string fileId)
    {
        var today = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var p = files.Combine("renders", today, fileId);
        return Task.FromResult((p, "application/pdf"));
    }
}
