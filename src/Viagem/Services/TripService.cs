using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;

namespace Viagem.Services;

public class TripService(ApplicationDbContext db) : ITripService
{
    public async Task<List<Trip>> GetUpcomingTripsAsync(string userId)
    {
        var now = DateTime.UtcNow.Date;
        return await db.Trips
            .Include(t => t.Destinations).ThenInclude(d => d.Place)
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .Where(t => t.EndDate >= now)
            .OrderBy(t => t.StartDate)
            .ToListAsync();
    }

    public async Task<List<Trip>> GetPastTripsAsync(string userId)
    {
        var cutoff = DateTime.UtcNow.AddYears(-1).Date;
        var now = DateTime.UtcNow.Date;
        return await db.Trips
            .Include(t => t.Destinations).ThenInclude(d => d.Place)
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .Where(t => t.EndDate < now && t.EndDate >= cutoff)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
    }

    public async Task<Trip?> GetTripAsync(int tripId, string userId)
    {
        return await db.Trips
            .Include(t => t.Destinations).ThenInclude(d => d.Place)
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Include(t => t.Transportations).ThenInclude(tr => tr.OriginPlace)
            .Include(t => t.Transportations).ThenInclude(tr => tr.DestinationPlace)
            .Include(t => t.Transportations).ThenInclude(tr => tr.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Include(t => t.Lodgings).ThenInclude(l => l.Place)
            .Include(t => t.Lodgings).ThenInclude(l => l.Travellers).ThenInclude(lt => lt.TravellerProfile)
            .Include(t => t.Activities).ThenInclude(a => a.Place)
            .Include(t => t.Activities).ThenInclude(a => a.Travellers).ThenInclude(at => at.TravellerProfile)
            .Include(t => t.Expenses)
            .Include(t => t.Attachments)
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .FirstOrDefaultAsync(t => t.Id == tripId);
    }

    public async Task<Trip> CreateTripAsync(Trip trip)
    {
        var ownerExists = await db.Users.AnyAsync(u => u.Id == trip.OwnerId);
        if (!ownerExists)
            throw new InvalidOperationException("Your session has expired. Please sign out and sign back in.");

        trip.CreatedAt = DateTime.UtcNow;
        trip.UpdatedAt = DateTime.UtcNow;
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        return trip;
    }

    public async Task<Trip> UpdateTripAsync(Trip trip)
    {
        trip.UpdatedAt = DateTime.UtcNow;
        var tracked = db.ChangeTracker.Entries<Trip>()
            .FirstOrDefault(e => e.Entity.Id == trip.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Trips.Update(trip);
        await db.SaveChangesAsync();
        return trip;
    }

    public async Task DeleteTripAsync(int tripId, string userId)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.OwnerId == userId);
        if (trip != null)
        {
            db.Trips.Remove(trip);
            await db.SaveChangesAsync();
        }
    }

    public async Task<bool> CanUserEditTripAsync(int tripId, string userId)
    {
        return await db.Trips.AnyAsync(t => t.Id == tripId && (
            t.OwnerId == userId ||
            t.Travellers.Any(tt => tt.CanEdit && tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)
        ));
    }

    public async Task<TripDestination> AddDestinationAsync(int tripId, int placeId)
    {
        var dest = new TripDestination { TripId = tripId, PlaceId = placeId };
        db.TripDestinations.Add(dest);
        await db.SaveChangesAsync();
        await db.Entry(dest).Reference(d => d.Place).LoadAsync();
        return dest;
    }

    public async Task<TripDestination> AddDestinationCustomAsync(int tripId, string customName)
    {
        var dest = new TripDestination { TripId = tripId, CustomName = customName.Trim() };
        db.TripDestinations.Add(dest);
        await db.SaveChangesAsync();
        return dest;
    }

    public async Task RemoveDestinationAsync(int destinationId)
    {
        var dest = await db.TripDestinations.FindAsync(destinationId);
        if (dest != null) { db.TripDestinations.Remove(dest); await db.SaveChangesAsync(); }
    }

    public async Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false)
    {
        var exists = await db.TripTravellers.AnyAsync(t => t.TripId == tripId && t.TravellerProfileId == travellerProfileId);
        if (!exists)
        {
            db.TripTravellers.Add(new TripTraveller { TripId = tripId, TravellerProfileId = travellerProfileId, CanEdit = isOrganiser || canEdit, IsOrganiser = isOrganiser });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveTravellerAsync(int tripTravellerId)
    {
        var t = await db.TripTravellers.FindAsync(tripTravellerId);
        if (t != null) { db.TripTravellers.Remove(t); await db.SaveChangesAsync(); }
    }

    public async Task SetTravellerEditAsync(int tripTravellerId, bool canEdit)
    {
        var t = await db.TripTravellers.FindAsync(tripTravellerId);
        if (t != null) { t.CanEdit = canEdit; await db.SaveChangesAsync(); }
    }

    public async Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser)
    {
        var t = await db.TripTravellers.FindAsync(tripTravellerId);
        if (t != null) { t.IsOrganiser = isOrganiser; if (isOrganiser) t.CanEdit = true; await db.SaveChangesAsync(); }
    }

    public async Task UpdateNotesAsync(int tripId, string? notes)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip != null) { trip.Notes = notes; trip.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
    }
}

public class PlaceService(ApplicationDbContext db) : IPlaceService
{
    public async Task<List<Place>> SearchAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var lower = query.ToLower();
        return await db.Places
            .Where(p => p.Name.ToLower().Contains(lower) || (p.CountryName != null && p.CountryName.ToLower().Contains(lower)))
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Place?> GetByIdAsync(int id) => await db.Places.FindAsync(id);
}

public class AirportService(ApplicationDbContext db) : IAirportService
{
    public async Task<List<Airport>> SearchAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.ToUpper().Trim();
        var lower = query.ToLower().Trim();
        // Prioritise IATA code matches, then name/municipality
        return await db.Airports
            .Where(a => a.IataCode.ToUpper().StartsWith(q)
                     || a.Name.ToLower().Contains(lower)
                     || (a.Municipality != null && a.Municipality.ToLower().Contains(lower)))
            .OrderByDescending(a => a.IataCode.ToUpper() == q)
            .ThenByDescending(a => a.IataCode.ToUpper().StartsWith(q))
            .ThenBy(a => a.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Airport?> GetByCodeAsync(string iataCode) =>
        await db.Airports.FirstOrDefaultAsync(a => a.IataCode.ToUpper() == iataCode.ToUpper().Trim());
}

public class AirlineService(ApplicationDbContext db) : IAirlineService
{
    public async Task<Airline?> GetByCodeAsync(string code) =>
        await db.Airlines.FirstOrDefaultAsync(a => a.Code.ToUpper() == code.ToUpper().Trim());

    public async Task<List<Airline>> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.ToUpper().Trim();
        var lower = query.ToLower().Trim();
        return await db.Airlines
            .Where(a => a.Code.ToUpper().StartsWith(q) || a.Name.ToLower().Contains(lower))
            .OrderByDescending(a => a.Code.ToUpper() == q)
            .ThenBy(a => a.Name)
            .Take(limit)
            .ToListAsync();
    }
}
