namespace DesignModule.Services.DTO;

public sealed class PreviewResponse
{
    public Guid VersionId { get; set; }
    public string PreviewPath { get; set; } = "";
}