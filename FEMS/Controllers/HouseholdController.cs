using System.Security.Claims;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Household;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Controllers
{
    [Authorize]
    public class HouseholdController : Controller
    {
        private readonly IHouseholdService _householdService;

        public HouseholdController(IHouseholdService householdService)
        {
            _householdService = householdService;
        }

        private int GetCurrentUserId()
        {
            string? val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(val, out int id) ? id : 0;
        }

        // =========================================
        // 1. TRANG CHỌN: "TẠO HỘ GIA ĐÌNH" HAY "THAM GIA HỘ GIA ĐÌNH"
        // GET: /Household/Setup
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            int userId = GetCurrentUserId();
            var activeMember = await _householdService.GetActiveMembershipAsync(userId);

            if (activeMember != null)
            {
                return RedirectToAction("Index", "Home");
            }

            var pendingInvites = await _householdService.GetPendingInvitesAsync(userId);
            ViewBag.PendingInviteCount = pendingInvites.Count;

            return View();
        }

        // =========================================
        // 2. TẠO HỘ GIA ĐÌNH (NHẬP TÊN HỘ -> RA TRANG HOME)
        // GET & POST: /Household/Create
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int userId = GetCurrentUserId();
            var activeMember = await _householdService.GetActiveMembershipAsync(userId);

            if (activeMember != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new CreateHouseholdViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateHouseholdViewModel model)
        {
            int userId = GetCurrentUserId();
            var activeMember = await _householdService.GetActiveMembershipAsync(userId);

            if (activeMember != null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _householdService.CreateHouseholdAsync(userId, model.HouseholdName);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", "Home");
        }

        // =========================================
        // 3. THAM GIA HỘ GIA ĐÌNH (TRANG CHỜ & NHẬP MÃ -> RA TRANG HOME)
        // GET & POST: /Household/Join
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Join()
        {
            int userId = GetCurrentUserId();
            var activeMember = await _householdService.GetActiveMembershipAsync(userId);

            if (activeMember != null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new JoinHouseholdViewModel
            {
                PendingInvites = await _householdService.GetPendingInvitesAsync(userId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinHouseholdViewModel model)
        {
            int userId = GetCurrentUserId();
            var activeMember = await _householdService.GetActiveMembershipAsync(userId);

            if (activeMember != null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                model.PendingInvites = await _householdService.GetPendingInvitesAsync(userId);
                return View(model);
            }

            var result = await _householdService.JoinHouseholdByCodeAsync(userId, model.InviteCode);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(model.InviteCode), result.Message);
                model.PendingInvites = await _householdService.GetPendingInvitesAsync(userId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", "Home");
        }

        // =========================================
        // 4. QUẢN LÝ HỘ GIA ĐÌNH (XEM THÀNH VIÊN, MỜI, THĂNG CẤP, XÓA)
        // GET: /Household/Index
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var dashboard = await _householdService.GetDashboardAsync(userId);

            if (dashboard == null)
            {
                return RedirectToAction(nameof(Setup));
            }

            return View(dashboard);
        }

        // POST: /Household/Invite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(string? inviteEmail)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.InviteMemberByEmailAsync(userId, inviteEmail ?? string.Empty);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                TempData["GeneratedInviteCode"] = result.InviteCode;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Household/CancelInvite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelInvite(int notificationId)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.CancelInviteAsync(userId, notificationId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Household/Promote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(int memberId)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.PromoteMemberAsync(userId, memberId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Household/Demote (Người tạo hộ gỡ quyền Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Demote(int memberId)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.DemoteMemberAsync(userId, memberId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Household/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int memberId)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.RemoveMemberAsync(userId, memberId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Household/Leave (Thành viên tham gia hộ tự rời khỏi hộ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave()
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.LeaveHouseholdAsync(userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Setup));
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Household/DeleteHousehold (Người tạo hộ xóa / giải tán hộ gia đình)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHousehold()
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.DeleteHouseholdAsync(userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Setup));
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 5. XEM VÍ CÁ NHÂN & LỊCH SỬ GIAO DỊCH CỦA THÀNH VIÊN (READ-ONLY) (UC-12, UC-16)
        // GET: /Household/MemberDetails/{id}
        // =========================================
        [HttpGet]
        public async Task<IActionResult> MemberDetails(
            int id,
            int? walletId = null,
            string? type = null,
            string? search = null,
            int page = 1)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.GetMemberFinanceDetailAsync(userId, id, walletId, type, search, page, 7);

            if (!result.Success || result.Data == null)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        // =========================================
        // 6. CẤU HÌNH HẠN MỨC CẢNH BÁO GIAO DỊCH CHO THÀNH VIÊN (UC-29 / BR-37)
        // POST: /Household/SetAlertThreshold
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAlertThreshold(
            int memberId,
            decimal? alertAmount,
            string? returnTo = null)
        {
            int userId = GetCurrentUserId();
            var result = await _householdService.SetMemberAlertThresholdAsync(userId, memberId, alertAmount);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            if (string.Equals(returnTo, "Details", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MemberDetails), new { id = memberId });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

