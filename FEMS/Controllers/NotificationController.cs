
using FEMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FEMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var notifications = await _notificationService
                .GetNotificationsAsync(userId);

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            bool success = await _notificationService
                .MarkAsReadAsync(userId, id);

            TempData["Message"] = success
            ? "Đã đánh dấu thông báo là đã đọc."
            : "Không thể đánh dấu thông báo này là đã đọc.";

            return RedirectToAction("Index");
        }
    }
}

