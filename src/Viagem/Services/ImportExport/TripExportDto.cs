using Viagem.Data.Models;

namespace Viagem.Services.ImportExport;

/// <summary>
/// Root DTO written as trip.json inside a Viagem export zip.
/// Only contains data that cannot be derived from the seed database.
/// </summary>
public class TripExportDto
{
    public string SchemaVersion { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? BudgetCurrency { get; set; }

    /// <summary>Relative path inside the zip to the cover image, e.g. "cover/image.jpg".</summary>
    public string? CoverImageZipPath { get; set; }

    public List<TripDestinationExportDto> Destinations { get; set; } = [];
    public List<TravellerProfileExportDto> Travellers { get; set; } = [];
    public List<TransportationExportDto> Transportations { get; set; } = [];
    public List<LodgingExportDto> Lodgings { get; set; } = [];
    public List<ActivityExportDto> Activities { get; set; } = [];
    public List<ExpenseExportDto> Expenses { get; set; } = [];
    public List<AttachmentExportDto> Attachments { get; set; } = [];
}

public class TripDestinationExportDto
{
    /// <summary>Seed place id — use for lookup only; null means custom name.</summary>
    public int? PlaceId { get; set; }
    public string? CustomName { get; set; }
}

public class TravellerProfileExportDto
{
    public string LegalName { get; set; } = "";
    public string? Email { get; set; }
    /// <summary>True if this traveller is the trip organiser.</summary>
    public bool IsOrganiser { get; set; }
    /// <summary>True if this traveller has edit rights.</summary>
    public bool CanEdit { get; set; }
    public List<string> Aliases { get; set; } = [];
}

public class TransportationExportDto
{
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
    // Car rental
    public string? RentalCompany { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropOffLocation { get; set; }
    // Parking
    public string? SpotNumber { get; set; }
    public string? ParkingAddress { get; set; }
    // Cost — stored here because the linked Expense will be recreated on import
    public decimal? CostAmount { get; set; }
    public string? CostCurrency { get; set; }
    public List<string> TravellerLegalNames { get; set; } = [];
}

public class LodgingExportDto
{
    public LodgingType Type { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? ConfirmationCode { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Timezone { get; set; }
    public decimal? CostAmount { get; set; }
    public string? CostCurrency { get; set; }
    public List<string> TravellerLegalNames { get; set; } = [];
}

public class ActivityExportDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Timezone { get; set; }
    public decimal? CostAmount { get; set; }
    public string? CostCurrency { get; set; }
    public List<string> TravellerLegalNames { get; set; } = [];
}

public class ExpenseExportDto
{
    public string Name { get; set; } = "";
    public ExpenseCategory? Category { get; set; }
    public string? Notes { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? OccurredOn { get; set; }
    /// <summary>Splits keyed by traveller legal name → amount.</summary>
    public Dictionary<string, decimal> Splits { get; set; } = [];
}

public class AttachmentExportDto
{
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    /// <summary>Relative path inside the zip.</summary>
    public string ZipPath { get; set; } = "";
}
