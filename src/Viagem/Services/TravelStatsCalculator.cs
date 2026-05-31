using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;

namespace Viagem.Services;

/// <summary>
/// Calculates and upserts travel statistics for all users (or a single user).
/// Only processes completed (past) trips.
/// </summary>
public class TravelStatsCalculator(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task RecalculateAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var userIds = await db.Trips
            .Where(t => t.EndDate < DateTime.UtcNow.Date)
            .Select(t => t.OwnerId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            if (ct.IsCancellationRequested) break;
            await RecalculateForUserAsync(userId, ct);
        }
    }

    public async Task RecalculateForUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow.Date;

        var trips = await db.Trips
            .Where(t => t.EndDate < now && (
                t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)))
            .Include(t => t.Destinations)
            .Include(t => t.Transportations)
            .Include(t => t.Lodgings)
            .Include(t => t.Activities)
            .Include(t => t.Expenses)
            .AsSplitQuery()
            .ToListAsync(ct);

        if (trips.Count == 0) return;

        var placeIds = trips.SelectMany(t => t.Destinations).Where(d => d.PlaceId.HasValue).Select(d => d.PlaceId!.Value).Distinct().ToList();
        var places = await db.Places.Where(p => placeIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var years = trips
            .Select(t => t.StartDate.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToList();

        // Upsert lifetime (year = 0) + per-year rows
        foreach (var year in years.Prepend(0))
        {
            var subset = year == 0 ? trips : trips.Where(t => t.StartDate.Year == year).ToList();
            var stats = BuildStats(userId, year, subset, places);
            await UpsertStatsAsync(db, stats, ct);
        }

        // Rebuild destination pins for user (lifetime)
        await RebuildDestinationsAsync(db, userId, trips, places, ct);

        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static UserTravelStats BuildStats(string userId, int year, List<Trip> trips, Dictionary<int, Place> places)
    {
        var tripCount = trips.Count;
        var totalDays = trips.Sum(t => (t.EndDate - t.StartDate).Days);

        var destNames = trips
            .SelectMany(t => t.Destinations)
            .Select(d => d.PlaceId.HasValue && places.TryGetValue(d.PlaceId.Value, out var p) ? p.Name : d.CustomName ?? "")
            .Where(n => n.Length > 0)
            .Distinct()
            .Count();

        // Transportation
        var transportLegs = trips.SelectMany(t => t.Transportations).ToList();
        var transportByType = transportLegs
            .GroupBy(tr => tr.Type.ToString())
            .Select(g =>
            {
                var tripsUsed = trips.Count(t => t.Transportations.Any(tr => tr.Type.ToString() == g.Key));
                var totalHours = g.Sum(tr => (tr.ArrivalTime - tr.DepartureTime).TotalHours);
                var avgHours = tripsUsed > 0 ? totalHours / tripsUsed : 0;
                return new TransportationTypeStat(g.Key, tripsUsed, Math.Round(totalHours, 1), Math.Round(avgHours, 1));
            })
            .OrderBy(s => s.Type)
            .ToList();

        var transportTotalHours = transportLegs.Sum(tr => (tr.ArrivalTime - tr.DepartureTime).TotalHours);
        var transportAvgHours = tripCount > 0 ? transportTotalHours / tripCount : 0;

        // Lodging
        var lodgings = trips.SelectMany(t => t.Lodgings).ToList();
        var lodgingByType = lodgings
            .GroupBy(l => l.Type.ToString())
            .Select(g => new LodgingTypeStat(g.Key, g.Sum(l => (l.EndDate - l.StartDate).Days)))
            .OrderBy(s => s.Type)
            .ToList();
        var totalNights = lodgings.Sum(l => (l.EndDate - l.StartDate).Days);

        // Activities
        var activityCount = trips.Sum(t => t.Activities.Count);
        var activityAvg = tripCount > 0 ? Math.Round((double)activityCount / tripCount, 1) : 0;

        // Expenses
        var expenses = trips.SelectMany(t => t.Expenses)
            .Where(e => e.Amount.HasValue && !string.IsNullOrWhiteSpace(e.Currency))
            .ToList();
        var byCurrency = expenses
            .GroupBy(e => e.Currency!)
            .Select(g => new CurrencyTotalStat(g.Key, g.Sum(e => e.Amount!.Value)))
            .OrderBy(c => c.Currency)
            .ToList();

        return new UserTravelStats
        {
            UserId = userId,
            Year = year,
            TripCount = tripCount,
            DestinationCount = destNames,
            TotalDays = totalDays,
            TransportationStatsJson = JsonSerializer.Serialize(transportByType, Json),
            TransportationTotalTrips = transportLegs.Count,
            TransportationTotalHours = Math.Round(transportTotalHours, 1),
            TransportationAvgHours = Math.Round(transportAvgHours, 1),
            LodgingStatsJson = JsonSerializer.Serialize(lodgingByType, Json),
            LodgingTotalNights = totalNights,
            ActivityTotalCount = activityCount,
            ActivityAvgPerTrip = activityAvg,
            ExpenseStatsJson = JsonSerializer.Serialize(byCurrency, Json),
            ExpenseCurrencyCount = byCurrency.Count,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static async Task UpsertStatsAsync(ApplicationDbContext db, UserTravelStats stats, CancellationToken ct)
    {
        var existing = await db.UserTravelStats
            .FirstOrDefaultAsync(s => s.UserId == stats.UserId && s.Year == stats.Year, ct);

        if (existing == null)
        {
            db.UserTravelStats.Add(stats);
        }
        else
        {
            existing.TripCount = stats.TripCount;
            existing.DestinationCount = stats.DestinationCount;
            existing.TotalDays = stats.TotalDays;
            existing.TransportationStatsJson = stats.TransportationStatsJson;
            existing.TransportationTotalTrips = stats.TransportationTotalTrips;
            existing.TransportationTotalHours = stats.TransportationTotalHours;
            existing.TransportationAvgHours = stats.TransportationAvgHours;
            existing.LodgingStatsJson = stats.LodgingStatsJson;
            existing.LodgingTotalNights = stats.LodgingTotalNights;
            existing.ActivityTotalCount = stats.ActivityTotalCount;
            existing.ActivityAvgPerTrip = stats.ActivityAvgPerTrip;
            existing.ExpenseStatsJson = stats.ExpenseStatsJson;
            existing.ExpenseCurrencyCount = stats.ExpenseCurrencyCount;
            existing.CalculatedAt = stats.CalculatedAt;
        }
    }

    private static async Task RebuildDestinationsAsync(
        ApplicationDbContext db, string userId, List<Trip> trips, Dictionary<int, Place> places, CancellationToken ct)
    {
        // Remove old pins for user
        var old = await db.UserTravelDestinations.Where(d => d.UserId == userId).ToListAsync(ct);
        db.UserTravelDestinations.RemoveRange(old);

        var seen = new HashSet<int>();
        var pins = new List<UserTravelDestination>();

        foreach (var dest in trips.SelectMany(t => t.Destinations))
        {
            if (dest.PlaceId.HasValue && places.TryGetValue(dest.PlaceId.Value, out var place) &&
                place.Latitude != null && place.Longitude != null &&
                seen.Add(place.Id))
            {
                pins.Add(new UserTravelDestination
                {
                    UserId = userId,
                    PlaceId = place.Id,
                    PlaceName = place.Name,
                    Latitude = place.Latitude,
                    Longitude = place.Longitude
                });
            }
        }

        if (pins.Count > 0)
            db.UserTravelDestinations.AddRange(pins);
    }
}
