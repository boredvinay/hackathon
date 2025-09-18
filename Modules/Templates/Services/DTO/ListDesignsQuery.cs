namespace DesignModule.Services.DTO;

public sealed class ListDesignsQuery
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}