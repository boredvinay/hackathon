using Microsoft.AspNetCore.Mvc;
using DesignModule.Services.DTO;
using DesignModule.Services.Interfaces;

namespace DesignModule.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class DesignsController(IDesignService svc) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    /// <summary>
    /// List all designs (headers) with optional filters. Defaults: page=1, pageSize=20.
    /// </summary>
    /// <param name="q">Search by Key (contains, case-insensitive)</param>
    /// <param name="status">Filter by Status (e.g., Active/Archived)</param>
    /// <param name="page">1-based page number</param>
    /// <param name="pageSize">Items per page (max 200)</param>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DesignListItem>>> ListDesigns(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await svc.ListDesignsAsync(new ListDesignsQuery
        {
            Q = q,
            Status = status,
            Page = page,
            PageSize = pageSize
        }, ct));

    // --------------- Designs ----------------

    /// <summary>
    /// Create a new design (header row) with a unique key.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateDesignResponse>> CreateDesign([FromBody] CreateDesignRequest req, CancellationToken ct)
    {
        var id = await svc.CreateDesignAsync(req, ct);
        return CreatedAtAction(nameof(GetDesign), new { id }, new CreateDesignResponse { Id = id });
    }

    /// <summary>
    /// Get a design header and optional latest version id.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DesignDto>> GetDesign(Guid id, CancellationToken ct)
        => Ok(await svc.GetDesignAsync(id, ct));

    /// <summary>List published versions (optionally filter by designId).</summary>
    [HttpGet("versions/published")]
    public async Task<ActionResult<IReadOnlyList<DesignVersionListItem>>> ListPublished([FromQuery] Guid? designId, CancellationToken ct)
        => Ok(await svc.ListPublishedAsync(designId, ct));

    // --------------- Versions ----------------

    /// <summary>Create a new design version in Draft state (stores initial DSL + optional schema doc).</summary>
    [HttpPost("{designId:guid}/versions")]
    public async Task<ActionResult<CreateDesignVersionResponse>> CreateVersion(Guid designId, [FromBody] CreateDesignVersionRequest req, CancellationToken ct)
    {
        var id = await svc.CreateVersionAsync(designId, req, ct);
        return CreatedAtAction(nameof(GetVersion), new { versionId = id }, new CreateDesignVersionResponse { Id = id });
    }

    /// <summary>Get a version header (includes state, semver, hashes) without the heavy DSL/Schema bodies.</summary>
    [HttpGet("versions/{versionId:guid}")]
    public async Task<ActionResult<DesignVersionDto>> GetVersion(Guid versionId, CancellationToken ct)
        => Ok(await svc.GetVersionAsync(versionId, ct));

    /// <summary>Get the DSL (canvas JSON) for a version.</summary>
    [HttpGet("versions/{versionId:guid}/dsl")]
    public async Task<ActionResult<string>> GetDsl(Guid versionId, CancellationToken ct)
        => Ok(await svc.GetDslAsync(versionId, ct));

    /// <summary>Replace the DSL for a Draft version.</summary>
    [HttpPut("versions/{versionId:guid}/dsl")]
    public async Task<IActionResult> SaveDsl(Guid versionId, [FromBody] SaveDslRequest req, CancellationToken ct)
    {
        await svc.SaveDslAsync(versionId, req, ct); 
        return NoContent();
    }

    /// <summary>Get the Schema document (meta + payloadSchema JSON) for a version.</summary>
    [HttpGet("versions/{versionId:guid}/schema")]
    public async Task<ActionResult<string>> GetSchema(Guid versionId, CancellationToken ct)
        => Ok(await svc.GetSchemaAsync(versionId, ct));

    /// <summary>Replace the Schema document for a Draft version.</summary>
    [HttpPut("versions/{versionId:guid}/schema")]
    public async Task<IActionResult> SaveSchema(Guid versionId, [FromBody] SaveSchemaRequest req, CancellationToken ct)
    {
        await svc.SaveSchemaAsync(versionId, req, ct);
        return NoContent();
    }

    /// <summary>Move Draft → InReview.</summary>
    [HttpPost("versions/{versionId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid versionId, CancellationToken ct)
    {
        await svc.SubmitAsync(versionId, ct);
        return Ok();
    }

    /// <summary>Approve (publish) a version. Draft/InReview → Published, writes Approvals row.</summary>
    [HttpPost("versions/{versionId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid versionId, [FromBody] ApproveRequest req, CancellationToken ct)
    {
        await svc.ApproveAsync(versionId, req, ct);
        return Ok();
    }

    /// <summary>Generate/refresh preview for a Draft/Published version (stores path in DesignVersions.PreviewPath).</summary>
    [HttpPost("versions/{versionId:guid}/preview")]
    public async Task<ActionResult<PreviewResponse>> Preview(Guid versionId, CancellationToken ct)
        => Ok(await svc.GeneratePreviewAsync(versionId, ct));
}
