using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class LodgingRepository(ApplicationDbContext db) : ILodgingRepository
{
    public async Task<List<Lodging>> GetByTripAsync(int tripId)
        => await db.Lodgings
            .Include(l => l.Place)
            .Include(l => l.Travellers).ThenInclude(lt => lt.TravellerProfile)
            .Include(l => l.Expense)
            .Where(l => l.TripId == tripId)
            .OrderBy(l => l.StartDate)
            .ToListAsync();

    public async Task<Lodging?> GetByIdAsync(int id)
        => await db.Lodgings
            .Include(l => l.Place)
            .Include(l => l.Travellers).ThenInclude(lt => lt.TravellerProfile)
            .Include(l => l.Attachments).ThenInclude(a => a.Attachment)
            .Include(l => l.Expense)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Lodging> CreateAsync(Lodging lodging)
    {
        lodging.CreatedAt = DateTime.UtcNow;
        lodging.UpdatedAt = DateTime.UtcNow;
        db.Lodgings.Add(lodging);
        await db.SaveChangesAsync();
        return lodging;
    }

    public async Task<Lodging> UpdateAsync(Lodging lodging)
    {
        lodging.UpdatedAt = DateTime.UtcNow;
        var tracked = db.ChangeTracker.Entries<Lodging>()
            .FirstOrDefault(e => e.Entity.Id == lodging.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Lodgings.Update(lodging);
        await db.SaveChangesAsync();
        return lodging;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.Lodgings.FindAsync(id);
        if (item != null)
        {
            db.Lodgings.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
