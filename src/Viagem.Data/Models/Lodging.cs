using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public enum LodgingType
{
    Hotel,
    Home,
    VacationRental,
    CampSite,
    Hostel,
    Other
}

public class Lodging
{
    public int Id { get; set; }

    [Required]
    public LodgingType Type { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Address { get; set; }
    public string? ConfirmationCode { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Timezone { get; set; }

    public decimal? CostAmount => Expense?.Amount;
    public string? CostCurrency => Expense?.Currency;

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    public int? PlaceId { get; set; }
    public Place? Place { get; set; }

    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LodgingAttachment> Attachments { get; set; } = [];
    public ICollection<LodgingTraveller> Travellers { get; set; } = [];
}

public class LodgingTraveller
{
    public int Id { get; set; }
    public int LodgingId { get; set; }
    public Lodging? Lodging { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }
}

public class LodgingAttachment
{
    public int Id { get; set; }
    public int LodgingId { get; set; }
    public Lodging? Lodging { get; set; }
    public int AttachmentId { get; set; }
    public TripAttachment? Attachment { get; set; }
}
