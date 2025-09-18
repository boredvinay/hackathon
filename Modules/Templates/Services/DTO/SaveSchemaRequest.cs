namespace DesignModule.Services.DTO;

public sealed class SaveSchemaRequest
{
    public string SchemaJson { get; set; } = ""; // full schema doc (meta + payloadSchema)
}