namespace Viagem.Services.ViewModels;

public record ActivityViewModel(
    int Id,
    int TripId,
    string Name,
    string? Description,
    string? Address,
    string? Notes,
    string? Link,
    DateTime StartDate,
    DateTime? EndDate,
    string? Timezone,
    decimal? CostAmount,
    string? CostCurrency,
    int? PlaceId,
    string? PlaceName,
    IReadOnlyList<TravellerProfileSummaryViewModel> Travellers);

public class ActivityFormModel
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public string? Timezone { get; set; }
    public decimal? CostAmount { get; set; }
    public string? CostCurrency { get; set; }
    public int? PlaceId { get; set; }
    public List<int> TravellerProfileIds { get; set; } = [];
}
