using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserAsync(string userId, bool unreadOnly = false);
    Task MarkReadAsync(int notificationId);
    Task MarkAllReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}
