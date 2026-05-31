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
    public Guid Id { get; set; } = Guid.NewGuid();

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

    // Cost properties removed (UI concern bleeding into model)

    public int? PlaceId { get; set; }

    public Guid? ExpenseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Guid> AttachmentIds { get; set; } = [];
    public List<int> TravellerProfileIds { get; set; } = [];
}
