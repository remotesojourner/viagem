using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class ActivityRepository(ApplicationDbContext db) : IActivityRepository
{
    public async Task<List<Activity>> GetByTripAsync(int tripId)
        => await db.Activities
            .Include(a => a.Place)
            .Include(a => a.Travellers).ThenInclude(at => at.TravellerProfile)
            .Include(a => a.Expense)
            .Where(a => a.TripId == tripId)
            .OrderBy(a => a.StartDate)
            .ToListAsync();

    public async Task<Activity?> GetByIdAsync(int id)
        => await db.Activities
            .Include(a => a.Place)
            .Include(a => a.Travellers).ThenInclude(at => at.TravellerProfile)
            .Include(a => a.Attachments).ThenInclude(aa => aa.Attachment)
            .Include(a => a.Expense)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Activity> CreateAsync(Activity activity)
    {
        activity.CreatedAt = DateTime.UtcNow;
        activity.UpdatedAt = DateTime.UtcNow;
        db.Activities.Add(activity);
        await db.SaveChangesAsync();
        return activity;
    }

    public async Task<Activity> UpdateAsync(Activity activity)
    {
        activity.UpdatedAt = DateTime.UtcNow;
        var tracked = db.ChangeTracker.Entries<Activity>()
            .FirstOrDefault(e => e.Entity.Id == activity.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Activities.Update(activity);
        await db.SaveChangesAsync();
        return activity;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.Activities.FindAsync(id);
        if (item != null)
        {
            db.Activities.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateTravellersAsync(int activityId, IEnumerable<int> travellerProfileIds)
    {
        var existing = await db.ActivityTravellers
            .Where(t => t.ActivityId == activityId)
            .ToListAsync();
        db.ActivityTravellers.RemoveRange(existing);
        var ids = travellerProfileIds.Distinct().ToList();
        db.ActivityTravellers.AddRange(
            ids.Select(id => new ActivityTraveller { ActivityId = activityId, TravellerProfileId = id }));
        await db.SaveChangesAsync();
    }
}
