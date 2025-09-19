namespace DesignModule.Services.DTO;

public sealed class CreateDesignResponse
{
    public Guid Id { get; set; }

    // Optionally include the initial version header and DSL so the client
    // can open the designer without extra round-trips.
    public DesignVersionDto? Version { get; set; }
    public string? DslJson { get; set; }
}