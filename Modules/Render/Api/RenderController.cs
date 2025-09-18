using Microsoft.AspNetCore.Mvc;
using RenderModule.Services.DTO;
using RenderModule.Services.Interfaces;

namespace RenderModule.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class RenderController(
    IRenderService render,
    IMergeService merge,
    IDiffService diff) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    // ---------- PREVIEW ----------
    [HttpPost("preview")]
    public async Task<ActionResult<RenderResultResponse>> Preview([FromBody] RenderPreviewRequest req, CancellationToken ct)
        => Ok(await render.PreviewAsync(req, ct));

    // ---------- SINGLE ----------
    [HttpPost("single")]
    public async Task<ActionResult<RenderResultResponse>> Single([FromBody] RenderSingleRequest req, CancellationToken ct)
    {
        string? idem = Request.Headers.TryGetValue("Idempotency-Key", out var v) ? v.ToString() : null;
        return Ok(await render.RenderSingleAsync(req, idem, ct));
    }

    // ---------- BATCH (async-ish stub – returns Completed for MVP) ----------
    [HttpPost("batch")]
    public async Task<ActionResult<RenderJobStatusResponse>> Batch([FromBody] RenderBatchRequest req, CancellationToken ct)
        => Ok(await render.RenderBatchAsync(req, ct));

    [HttpGet("batch/{jobId:guid}")]
    public async Task<ActionResult<RenderJobStatusResponse>> BatchStatus(Guid jobId, CancellationToken ct)
        => Ok(await render.GetBatchStatusAsync(jobId, ct));

    // ---------- FILES ----------
    [HttpGet("files/{fileId}")]
    public async Task<IActionResult> Download(string fileId, CancellationToken ct)
    {
        var (path, contentType) = await render.ResolveFileAsync(fileId, ct);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, contentType ?? "application/octet-stream", enableRangeProcessing: true);
    }

    // ---------- NEW: MERGE PDF ----------
    [HttpPost("merge-pdf")]
    public async Task<ActionResult<RenderResultResponse>> MergePdf([FromBody] MergePdfRequest req, CancellationToken ct)
        => Ok(await merge.MergeAsync(req, ct));

    // ---------- NEW: DIFF (version A vs B) ----------
    [HttpPost("diff")]
    public async Task<ActionResult<DiffResponse>> Diff([FromBody] DiffRequest req, CancellationToken ct)
        => Ok(await diff.DiffAsync(req, ct));
}
