using RenderModule.Services.DTO;
using RenderModule.Engines;
using SharedModule;
using RenderModule.Providers;
using RenderModule.Services.Interfaces;

namespace RenderModule.Services;

public sealed class RenderService(
    IDesignReadProvider designReader,
    IPdfEngine pdf,
    IFileStore files) : IRenderService
{
    public async Task<RenderResultResponse> PreviewAsync(RenderPreviewRequest req, CancellationToken ct)
    {
        var dsl = await designReader.GetDslAsync(req.DesignVersionId, ct)
                  ?? throw new InvalidOperationException("DSL not found for version.");

        var bytes = pdf.RenderPdf(dsl, req.Payload);

        var outId = Guid.NewGuid();
        var outPath = files.Combine("renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), $"{outId}.pdf");
        await files.WriteAllBytesAsync(outPath, bytes, ct);

        return new RenderResultResponse { RenderId = outId, ContentType = "application/pdf", Url = $"/api/render/files/{outId}.pdf" };
    }

    public async Task<RenderResultResponse> RenderSingleAsync(RenderSingleRequest req, string? idemKey, CancellationToken ct)
    {
        var dsl = await designReader.GetDslAsync(req.DesignVersionId, ct)
                  ?? throw new InvalidOperationException("DSL not found for version.");

        // TODO: validate req.Payload against schema via designReader.GetSchemaAsync(req.DesignVersionId, ct)
        var bytes = pdf.RenderPdf(dsl, req.Payload);

        var id = Guid.NewGuid();
        var path = files.Combine("renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), $"{id}.pdf");
        await files.WriteAllBytesAsync(path, bytes, ct);

        return new RenderResultResponse { RenderId = id, ContentType = "application/pdf", Url = $"/api/render/files/{id}.pdf" };
    }

    public async Task<RenderJobStatusResponse> RenderBatchAsync(RenderBatchRequest req, CancellationToken ct)
    {
        var dsl = await designReader.GetDslAsync(req.DesignVersionId, ct)
                  ?? throw new InvalidOperationException("DSL not found for version.");

        var jobId = Guid.NewGuid();
        var root = files.Combine("renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), jobId.ToString("N"));
        await files.CreateDirectoryAsync(root, ct);

        var statuses = new List<RenderBatchItemStatus>();
        foreach (var item in req.Items)
        {
            try
            {
                var bytes = pdf.RenderPdf(dsl, item.Payload);
                var filePath = Path.Combine(root, $"{Sanitize(item.Id)}.pdf");
                await File.WriteAllBytesAsync(filePath, bytes, ct);
                statuses.Add(new RenderBatchItemStatus { Id = item.Id, Status = "Completed" });
            }
            catch (Exception ex)
            {
                statuses.Add(new RenderBatchItemStatus { Id = item.Id, Status = "Failed", Error = ex.Message });
            }
        }

        // Optionally zip here (left as exercise)
        var resultUrl = $"/api/render/files/{jobId}.zip"; // placeholder URL for a later bundler

        return new RenderJobStatusResponse
        {
            JobId = jobId,
            Status = statuses.Any(s => s.Status == "Failed") ? "Failed" : "Completed",
            ResultUrl = resultUrl,
            Items = statuses
        };
    }

    public Task<RenderJobStatusResponse> GetBatchStatusAsync(Guid jobId, CancellationToken ct)
        => Task.FromResult(new RenderJobStatusResponse { JobId = jobId, Status = "Completed", ResultUrl = $"/api/render/files/{jobId}.zip", Items = new() });

    public Task<(string path, string? contentType)> ResolveFileAsync(string fileId, CancellationToken ct)
    {
        var today = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var p = files.Combine("renders", today, fileId);
        if (File.Exists(p))
            return Task.FromResult<(string, string?)>((p, fileId.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/zip"));

        // naive fallback search (yesterday)
        var y = DateTime.UtcNow.AddDays(-1).ToString("yyyy/MM/dd");
        var py = files.Combine("renders", y, fileId);
        return Task.FromResult<(string, string?)>((py, null));
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
}
