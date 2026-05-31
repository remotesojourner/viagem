using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class TripRepository(ApplicationDbContext db) : ITripRepository
{
    public async Task<PagedResult<Trip>> GetUpcomingPagedAsync(string userId, int page, int pageSize, string filter = "all")
    {
        var now = DateTime.UtcNow.Date;
        var query = db.Trips
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .Where(t => t.EndDate >= now);

        query = filter switch
        {
            "traveller" => query.Where(t => t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)),
            "organiser" => query.Where(t => t.OwnerId == userId && !t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)),
            _ => query
        };

        var total = await query.CountAsync();
        var items = await query
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .OrderBy(t => t.StartDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return new PagedResult<Trip> { Items = items, TotalCount = total };
    }

    public async Task<PagedResult<Trip>> GetPastPagedAsync(string userId, int page, int pageSize, string filter = "all")
    {
        var now = DateTime.UtcNow.Date;
        var query = db.Trips
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .Where(t => t.EndDate < now);

        query = filter switch
        {
            "traveller" => query.Where(t => t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)),
            "organiser" => query.Where(t => t.OwnerId == userId && !t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)),
            _ => query
        };

        var total = await query.CountAsync();
        var items = await query
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .OrderByDescending(t => t.StartDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return new PagedResult<Trip> { Items = items, TotalCount = total };
    }

    public async Task<Trip?> GetByIdAsync(int tripId, string userId)
    {
        return await db.Trips
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == tripId);
    }

    public async Task<Trip> CreateAsync(Trip trip)
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

    public async Task<Trip> UpdateAsync(Trip trip)
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

    public async Task<Trip> UpdateWithRelationsAsync(Trip trip, IEnumerable<TripDestination> destinations, IEnumerable<TripTraveller> travellers)
    {
        trip.UpdatedAt = DateTime.UtcNow;

        // Clear and refill owned JSON collection
        trip.Destinations.Clear();
        foreach (var d in destinations) trip.Destinations.Add(d);

        db.Trips.Update(trip);

        var existingTravellers = await db.TripTravellers.Where(t => t.TripId == trip.Id).ToListAsync();
        db.TripTravellers.RemoveRange(existingTravellers);
        db.TripTravellers.AddRange(travellers.Select(t => { t.TripId = trip.Id; return t; }));

        await db.SaveChangesAsync();
        return trip;
    }

    public async Task DeleteAsync(int tripId, string userId)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.OwnerId == userId);
        if (trip != null)
        {
            db.Trips.Remove(trip);
            await db.SaveChangesAsync();
        }
    }

    public async Task<bool> CanEditAsync(int tripId, string userId)
    {
        return await db.Trips.AnyAsync(t => t.Id == tripId && (
            t.OwnerId == userId ||
            t.Travellers.Any(tt => tt.CanEdit && tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId)
        ));
    }

    public async Task<TripDestination> AddDestinationAsync(int tripId, int placeId)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip == null) throw new InvalidOperationException("Trip not found");
        var dest = new TripDestination { PlaceId = placeId };
        trip.Destinations.Add(dest);
        await db.SaveChangesAsync();
        return dest;
    }

    public async Task<TripDestination> AddDestinationCustomAsync(int tripId, string customName)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip == null) throw new InvalidOperationException("Trip not found");
        var dest = new TripDestination { CustomName = customName.Trim() };
        trip.Destinations.Add(dest);
        await db.SaveChangesAsync();
        return dest;
    }

    public async Task RemoveDestinationAsync(int tripId, Guid destinationId)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip == null) return;

        var dest = trip.Destinations.FirstOrDefault(d => d.Id == destinationId);
        if (dest != null)
        {
            trip.Destinations.Remove(dest);
            await db.SaveChangesAsync();
        }
    }

    public async Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false)
    {
        var exists = await db.TripTravellers.AnyAsync(t => t.TripId == tripId && t.TravellerProfileId == travellerProfileId);
        if (!exists)
        {
            db.TripTravellers.Add(new TripTraveller
            {
                TripId = tripId,
                TravellerProfileId = travellerProfileId,
                CanEdit = isOrganiser || canEdit,
                IsOrganiser = isOrganiser
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveTravellerAsync(int tripTravellerId)
    {
        var t = await db.TripTravellers.FindAsync(tripTravellerId);
        if (t != null)
        {
            db.TripTravellers.Remove(t);
            await db.SaveChangesAsync();
        }
    }

    public async Task SetTravellerEditAsync(int tripTravellerId, bool canEdit)
    {
        var t = await db.TripTravellers.FindAsync(tripTravellerId);
        if (t != null)
        {
            t.CanEdit = canEdit;
            await db.SaveChangesAsync();
        }
    }

    public async Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser)
    {
        var t = await db.TripTravellers.FindAsync(tripTravellerId);
        if (t != null)
        {
            t.IsOrganiser = isOrganiser;
            if (isOrganiser) t.CanEdit = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateNotesAsync(int tripId, string? notes)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip != null)
        {
            trip.Notes = notes;
            trip.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateCoverImageAsync(int tripId, string? coverImagePath)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip != null)
        {
            trip.CoverImagePath = coverImagePath;
            trip.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
