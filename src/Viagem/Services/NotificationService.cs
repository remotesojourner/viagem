using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class NotificationService(INotificationRepository repo) : INotificationService
{
    public async Task<List<NotificationViewModel>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var items = await repo.GetByUserAsync(userId, unreadOnly);
        return items.Select(ToViewModel).ToList();
    }

    public Task MarkAsReadAsync(int notificationId) => repo.MarkReadAsync(notificationId);

    public Task MarkAllAsReadAsync(string userId) => repo.MarkAllReadAsync(userId);

    public Task<int> GetUnreadCountAsync(string userId) => repo.GetUnreadCountAsync(userId);

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static NotificationViewModel ToViewModel(Notification n)
        => new(n.Id, n.Subject, n.Message, n.Sender, n.Read, n.CreatedAt);
}
