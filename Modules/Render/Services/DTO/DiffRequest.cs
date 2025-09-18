namespace RenderModule.Services.DTO;

public sealed class DiffRequest
{
    public Guid VersionA { get; set; }
    public Guid VersionB { get; set; }
    public string Mode { get; set; } = "pixel"; // "pixel" | "dsl"
}