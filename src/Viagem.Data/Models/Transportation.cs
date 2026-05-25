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
    public int Id { get; set; }

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

    public decimal? CostAmount => Expense?.Amount;
    public string? CostCurrency => Expense?.Currency;

    // Car rental specific
    public string? RentalCompany { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropOffLocation { get; set; }

    // Parking specific
    public string? SpotNumber { get; set; }
    public string? ParkingAddress { get; set; }

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    public int? OriginPlaceId { get; set; }
    public Place? OriginPlace { get; set; }

    public int? DestinationPlaceId { get; set; }
    public Place? DestinationPlace { get; set; }

    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TransportationAttachment> Attachments { get; set; } = [];
    public ICollection<TransportationTraveller> Travellers { get; set; } = [];
}

public class TransportationTraveller
{
    public int Id { get; set; }
    public int TransportationId { get; set; }
    public Transportation? Transportation { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }
}

public class TransportationAttachment
{
    public int Id { get; set; }
    public int TransportationId { get; set; }
    public Transportation? Transportation { get; set; }
    public int AttachmentId { get; set; }
    public TripAttachment? Attachment { get; set; }
}
