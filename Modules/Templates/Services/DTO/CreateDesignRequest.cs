namespace DesignModule.Services.DTO;

public sealed class CreateDesignRequest
{
    public string Key { get; set; } = "";     // unique human key, e.g., "ShippingLabel"
    public string CreatedBy { get; set; } = "system";
}