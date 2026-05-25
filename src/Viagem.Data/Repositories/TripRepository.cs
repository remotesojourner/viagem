using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class TripRepository(ApplicationDbContext db) : ITripRepository
{
    public async Task<PagedResult<TripSummaryRow>> GetUpcomingPagedAsync(string userId, int page, int pageSize, string filter = "all")
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
            .OrderBy(t => t.StartDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(t => new TripSummaryRow
            {
                Id = t.Id,
                Name = t.Name,
                CoverImagePath = t.CoverImagePath,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                OwnerId = t.OwnerId,
                CurrentUserIsTraveller = t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId),
                DestinationNames = t.Destinations
                    .Select(d => d.CustomName != null ? d.CustomName : d.Place != null ? d.Place.Name : "")
                    .Where(n => n != "")
                    .ToList()
            })
            .ToListAsync();

        return new PagedResult<TripSummaryRow> { Items = items, TotalCount = total };
    }

    public async Task<PagedResult<TripSummaryRow>> GetPastPagedAsync(string userId, int page, int pageSize, string filter = "all")
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
            .OrderByDescending(t => t.StartDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(t => new TripSummaryRow
            {
                Id = t.Id,
                Name = t.Name,
                CoverImagePath = t.CoverImagePath,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                OwnerId = t.OwnerId,
                CurrentUserIsTraveller = t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId),
                DestinationNames = t.Destinations
                    .Select(d => d.CustomName != null ? d.CustomName : d.Place != null ? d.Place.Name : "")
                    .Where(n => n != "")
                    .ToList()
            })
            .ToListAsync();

        return new PagedResult<TripSummaryRow> { Items = items, TotalCount = total };
    }

    public async Task<Trip?> GetByIdAsync(int tripId, string userId)
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
        if (dest != null)
        {
            db.TripDestinations.Remove(dest);
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
