namespace DesignModule.Services.DTO;

public sealed class CreateDesignVersionRequest
{
    public string SemVer { get; set; } = "1.0.0";   // semantic version you want for this version
    public string CreatedBy { get; set; } = "system";
    public string? DslJson { get; set; }
    public string? SchemaJson { get; set; }
}