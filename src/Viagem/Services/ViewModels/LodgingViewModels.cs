using Viagem.Data.Models;

namespace Viagem.Services.ViewModels;

public record LodgingViewModel(
    int Id,
    int TripId,
    LodgingType Type,
    string Name,
    string? Address,
    string? ConfirmationCode,
    string? Notes,
    string? Link,
    DateTime StartDate,
    DateTime EndDate,
    string? Timezone,
    decimal? CostAmount,
    string? CostCurrency,
    int? PlaceId,
    string? PlaceName,
    IReadOnlyList<TravellerProfileSummaryViewModel> Travellers);

public class LodgingFormModel
{
    public LodgingType Type { get; set; } = LodgingType.Hotel;
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? ConfirmationCode { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);
    public string? Timezone { get; set; }
    public decimal? CostAmount { get; set; }
    public string? CostCurrency { get; set; }
    public int? PlaceId { get; set; }
    public List<int> TravellerProfileIds { get; set; } = [];
}
