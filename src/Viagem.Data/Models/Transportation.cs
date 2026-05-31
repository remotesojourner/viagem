using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public enum TransportationType
{
    Flight,
    Train,
    Bus,
    Car,
    CarRental,
    Ferry,
    Taxi,
    Bike,
    Walk,
    Parking,
    Other
}

public class Transportation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public TransportationType Type { get; set; }

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

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string? DepartureTimezone { get; set; }
    public string? ArrivalTimezone { get; set; }

    // Cost properties removed (UI concern bleeding into model)

    // Car rental specific
    public string? RentalCompany { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropOffLocation { get; set; }

    // Parking specific
    public string? SpotNumber { get; set; }
    public string? ParkingAddress { get; set; }

    public int? OriginPlaceId { get; set; }

    public int? DestinationPlaceId { get; set; }

    public Guid? ExpenseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Guid> AttachmentIds { get; set; } = [];
    public List<int> TravellerProfileIds { get; set; } = [];
}
