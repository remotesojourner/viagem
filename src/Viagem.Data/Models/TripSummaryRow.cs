namespace Viagem.Data.Models;

public sealed class TripSummaryRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CoverImagePath { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public bool CurrentUserIsTraveller { get; init; }
    public List<string> DestinationNames { get; init; } = [];
}

public sealed class PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
