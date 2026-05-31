using Viagem.Data.Models;

namespace Viagem.Services.ViewModels;

public record TransportationViewModel(
    Guid Id,
    int TripId,
    TransportationType Type,
    string? Origin,
    string? OriginCity,
    string? Destination,
    string? DestinationCity,
    string? Provider,
    string? ConfirmationCode,
    string? FlightNumber,
    string? AssignedSeats,
    string? Notes,
    string? Link,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string? DepartureTimezone,
    string? ArrivalTimezone,
    decimal? CostAmount,
    string? CostCurrency,
    string? RentalCompany,
    string? PickupLocation,
    string? DropOffLocation,
    string? SpotNumber,
    string? ParkingAddress,
    int? OriginPlaceId,
    string? OriginPlaceName,
    int? DestinationPlaceId,
    string? DestinationPlaceName,
    IReadOnlyList<TravellerProfileSummaryViewModel> Travellers);

public class TransportationFormModel
{
    public TransportationType Type { get; set; } = TransportationType.Flight;
    public string? Origin { get; set; }
    public string? OriginCity { get; set; }
    public string? Destination { get; set; }
    public string? DestinationCity { get; set; }
    public string? Provider { get; set; }
    public string? ConfirmationCode { get; set; }
    public string? FlightNumber { get; set; }
    public string? AssignedSeats { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }
    public DateTime DepartureTime { get; set; } = DateTime.Today;
    public DateTime ArrivalTime { get; set; } = DateTime.Today;
    public string? DepartureTimezone { get; set; }
    public string? ArrivalTimezone { get; set; }
    public decimal? CostAmount { get; set; }
    public string? CostCurrency { get; set; }
    public string? RentalCompany { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropOffLocation { get; set; }
    public string? SpotNumber { get; set; }
    public string? ParkingAddress { get; set; }
    public int? OriginPlaceId { get; set; }
    public int? DestinationPlaceId { get; set; }
    public List<int> TravellerProfileIds { get; set; } = [];
}
