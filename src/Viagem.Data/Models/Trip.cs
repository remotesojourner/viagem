using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public class Trip
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }
    public string? Notes { get; set; }

    [Required]
    public string OwnerId { get; set; } = "";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? CoverImagePath { get; set; }

    public decimal? BudgetAmount { get; set; }
    public string? BudgetCurrency { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ApplicationUser? Owner { get; set; }
    public ICollection<TripDestination> Destinations { get; set; } = [];
    public ICollection<TripTraveller> Travellers { get; set; } = [];
    public ICollection<Transportation> Transportations { get; set; } = [];
    public ICollection<Lodging> Lodgings { get; set; } = [];
    public ICollection<Activity> Activities { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<TripAttachment> Attachments { get; set; } = [];
}

public class TripDestination
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public Trip? Trip { get; set; }
    public int? PlaceId { get; set; }
    public Place? Place { get; set; }
    public string? CustomName { get; set; }

    public string DisplayName => Place?.Name ?? CustomName ?? "Unknown";
}

public class TripTraveller
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public Trip? Trip { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }
    public bool CanEdit { get; set; } = false;
    public bool IsOrganiser { get; set; } = false;
}
