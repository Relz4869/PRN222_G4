
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;

namespace FEMS.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IHouseholdRepository _householdRepository;

        public NotificationService(IHouseholdRepository householdRepository)
        {
            _householdRepository = householdRepository;
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId)
        {
            return await _householdRepository
                .GetNotificationsByUserIdAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
        {
            var notification = await _householdRepository
                .GetNotificationByIdAsync(notificationId);

            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            if (!notification.IsRead &&
                notification.Message.StartsWith("[INVITE:"))
            {
                return false;
            }

            if (notification.IsRead)
            {
                return true;
            }

            notification.IsRead = true;

            await _householdRepository
                .UpdateNotificationAsync(notification);

            return true;
        }
    }
}
