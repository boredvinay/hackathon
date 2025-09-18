namespace RenderModule.Services.DTO;

public sealed class DesignVersionHead
{
    public Guid Id { get; set; }
    public Guid DesignId { get; set; }
    public string SemVer { get; set; } = "1.0.0";
    public string State { get; set; } = "Published";
    public string? PreviewPath { get; set; }
}