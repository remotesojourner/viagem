using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class TransportationRepository(ApplicationDbContext db) : ITransportationRepository
{
    public async Task<List<Transportation>> GetByTripAsync(int tripId)
        => await db.Transportations
            .Include(t => t.OriginPlace)
            .Include(t => t.DestinationPlace)
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Include(t => t.Expense)
            .Where(t => t.TripId == tripId)
            .OrderBy(t => t.DepartureTime)
            .ToListAsync();

    public async Task<Transportation?> GetByIdAsync(int id)
        => await db.Transportations
            .Include(t => t.OriginPlace)
            .Include(t => t.DestinationPlace)
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Include(t => t.Attachments).ThenInclude(a => a.Attachment)
            .Include(t => t.Expense)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Transportation> CreateAsync(Transportation transportation)
    {
        transportation.CreatedAt = DateTime.UtcNow;
        transportation.UpdatedAt = DateTime.UtcNow;
        db.Transportations.Add(transportation);
        await db.SaveChangesAsync();
        return transportation;
    }

    public async Task<Transportation> UpdateAsync(Transportation transportation)
    {
        transportation.UpdatedAt = DateTime.UtcNow;
        var tracked = db.ChangeTracker.Entries<Transportation>()
            .FirstOrDefault(e => e.Entity.Id == transportation.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Transportations.Update(transportation);
        await db.SaveChangesAsync();
        return transportation;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.Transportations.FindAsync(id);
        if (item != null)
        {
            db.Transportations.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateTravellersAsync(int transportationId, IEnumerable<int> travellerProfileIds)
    {
        var existing = await db.TransportationTravellers
            .Where(t => t.TransportationId == transportationId)
            .ToListAsync();
        db.TransportationTravellers.RemoveRange(existing);
        var ids = travellerProfileIds.Distinct().ToList();
        db.TransportationTravellers.AddRange(
            ids.Select(id => new TransportationTraveller { TransportationId = transportationId, TravellerProfileId = id }));
        await db.SaveChangesAsync();
    }
}
