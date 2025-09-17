using System.Text.Json.Serialization;

namespace JobsModule.Services.DTO;

/// <summary>
/// Creates a job against a specific design version (DB column: TemplateVersionId).
/// Accepts both "designVersionId" (new) and "templateVersionId" (legacy) in JSON.
/// </summary>
public sealed class CreateJobRequest
{
    /// <summary>
    /// Preferred JSON key going forward.
    /// </summary>
    [JsonPropertyName("designVersionId")]
    public Guid? DesignVersionId { get; set; }

    /// <summary>
    /// Legacy JSON key we still accept for backward compatibility.
    /// </summary>
    [JsonPropertyName("templateVersionId")]
    public Guid? TemplateVersionIdCompat { get; set; }

    /// <summary>
    /// Optional. If not provided, inferred: Single when items<=1 else Bulk.
    /// </summary>
    public string? Type { get; set; }  // "Single" | "Bulk"

    /// <summary>
    /// Optional webhook to notify on completion/failure.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Job payloads. Each dictionary becomes JobItems.PayloadJson.
    /// </summary>
    public List<Dictionary<string, object>> Items { get; set; } = [];

    /// <summary>
    /// Resolve the effective design version id (maps to DB: TemplateVersionId).
    /// </summary>
    [JsonIgnore]
    public Guid EffectiveDesignVersionId =>
        DesignVersionId ?? TemplateVersionIdCompat
        ?? throw new InvalidOperationException("designVersionId (or templateVersionId) is required.");
}