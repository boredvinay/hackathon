namespace DesignModule.Services.DTO;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}