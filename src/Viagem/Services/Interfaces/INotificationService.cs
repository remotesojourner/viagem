using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface INotificationService
{
    Task<List<NotificationViewModel>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}
