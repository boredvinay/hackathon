namespace DesignModule.Services.DTO;

public sealed class DesignVersionDto
{
    public Guid Id { get; set; }
    public Guid DesignId { get; set; }
    public string SemVer { get; set; } = "1.0.0";
    public string State { get; set; } = "Draft"; // Draft | InReview | Published | Archived
    public string Sha256 { get; set; } = "";
    public string? PreviewPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
}