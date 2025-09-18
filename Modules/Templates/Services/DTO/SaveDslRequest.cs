namespace DesignModule.Services.DTO;

public sealed class SaveDslRequest
{
    public string DslJson { get; set; } = ""; // full DSL string (canvas)
}