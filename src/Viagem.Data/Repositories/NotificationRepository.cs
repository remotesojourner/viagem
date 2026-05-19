using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class NotificationRepository(ApplicationDbContext db) : INotificationRepository
{
    public async Task<List<Notification>> GetByUserAsync(string userId, bool unreadOnly = false)
    {
        var query = db.Notifications
            .Where(n => n.UserId == userId)
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

        if (unreadOnly)
            query = query.Where(n => !n.Read);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task MarkReadAsync(int notificationId)
    {
        var n = await db.Notifications.FindAsync(notificationId);
        if (n != null)
        {
            n.Read = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(string userId)
    {
        await db.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true));
    }

    public async Task<int> GetUnreadCountAsync(string userId)
        => await db.Notifications.CountAsync(n => n.UserId == userId && !n.Read);
}
