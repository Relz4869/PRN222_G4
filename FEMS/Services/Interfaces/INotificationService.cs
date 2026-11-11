
using FEMS.Models.Entities;

namespace FEMS.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsAsync(int userId);

        Task<bool> MarkAsReadAsync(int userId, int notificationId);
    }
}
