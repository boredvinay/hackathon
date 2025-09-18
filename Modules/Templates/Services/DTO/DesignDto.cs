namespace DesignModule.Services.DTO;

public sealed class DesignDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public Guid? LatestVersionId { get; set; }
}