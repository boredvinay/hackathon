using Microsoft.AspNetCore.Mvc;
using JobsModule.Services;
using JobsModule.Services.DTO;

namespace JobsModule.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController(IJobService jobs) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    [HttpPost]
    public async Task<ActionResult<CreateJobResponse>> Create([FromBody] CreateJobRequest req, CancellationToken ct)
    {
        try
        {
            var idem = Request.Headers["Idempotency-Key"].FirstOrDefault();
            var id = await jobs.CreateAsync(req, idem, ct);
            return CreatedAtAction(nameof(Get), new { id }, new CreateJobResponse { Id = id });
        }
        catch (InvalidOperationException ex) // thrown when TemplateVersionId is invalid
        {
            return BadRequest(new
            {
                error = "InvalidTemplateVersionId",
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobStatusResponse>> Get(Guid id, CancellationToken ct)
        => Ok(await jobs.GetStatusAsync(id, ct));

    [HttpGet("{id:guid}/result")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (stream, name) = await jobs.GetResultAsync(id, ct);
        return stream is null ? NotFound() : File(stream, "application/zip", name);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => await jobs.CancelAsync(id, ct) ? Ok() : Conflict("Job cannot be cancelled.");
}