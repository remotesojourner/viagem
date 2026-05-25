using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Viagem.Data.Models;

/// <summary>
/// Pre-calculated travel statistics for a user, scoped to a specific year or lifetime (Year = 0).
/// Populated by the background stats job; only covers completed (past) trips.
/// </summary>
public class UserTravelStats
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = "";

    /// <summary>Year the stats cover. 0 = lifetime aggregate.</summary>
    public int Year { get; set; }

    // ── Summary ──────────────────────────────────────────────────────────────
    public int TripCount { get; set; }
    public int DestinationCount { get; set; }
    public int TotalDays { get; set; }

    // ── Transportation ───────────────────────────────────────────────────────
    /// <summary>JSON: List&lt;TransportationTypeStat&gt;</summary>
    public string TransportationStatsJson { get; set; } = "[]";

    public int TransportationTotalTrips { get; set; }
    public double TransportationTotalHours { get; set; }
    public double TransportationAvgHours { get; set; }

    // ── Lodging ───────────────────────────────────────────────────────────────
    /// <summary>JSON: List&lt;LodgingTypeStat&gt;</summary>
    public string LodgingStatsJson { get; set; } = "[]";

    public int LodgingTotalNights { get; set; }

    // ── Activities ────────────────────────────────────────────────────────────
    public int ActivityTotalCount { get; set; }
    public double ActivityAvgPerTrip { get; set; }

    // ── Expenses ──────────────────────────────────────────────────────────────
    /// <summary>JSON: List&lt;CurrencyTotalStat&gt;</summary>
    public string ExpenseStatsJson { get; set; } = "[]";

    public int ExpenseCurrencyCount { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>
/// Stores a visited destination (with coordinates) per user for the world-map pins.
/// Scoped to completed trips only.
/// </summary>
public class UserTravelDestination
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = "";

    public int? PlaceId { get; set; }
    public Place? Place { get; set; }

    [Required]
    public string PlaceName { get; set; } = "";

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}

// ── Embedded JSON DTOs ────────────────────────────────────────────────────────

public sealed record TransportationTypeStat(
    string Type,
    int TripCount,
    double TotalHours,
    double AvgHours);

public sealed record LodgingTypeStat(
    string Type,
    int Nights);

public sealed record CurrencyTotalStat(
    string Currency,
    decimal Total);
