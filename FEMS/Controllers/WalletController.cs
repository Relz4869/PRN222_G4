using System.Security.Claims;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        private int GetCurrentUserId()
        {
            string? val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(val, out int id) ? id : 0;
        }

        // =========================================
        // 1. XEM DANH SÁCH VÍ CÁ NHÂN & LỊCH SỬ BIẾN ĐỘNG VÍ (View Personal Wallets)
        // GET: /Wallet/Index?walletId={id}
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Index(int? walletId = null)
        {
            int userId = GetCurrentUserId();
            var model = await _walletService.GetWalletManagementDataAsync(userId, walletId);

            if (!model.HasHouseholdMembership)
            {
                TempData["ErrorMessage"] = "Vui lòng tạo hoặc tham gia một Hộ gia đình trước khi quản lý Ví cá nhân.";
                return RedirectToAction("Setup", "Household");
            }

            return View(model);
        }

        // =========================================
        // 2. TẠO VÍ CÁ NHÂN MỚI (Create Personal Wallet)
        // POST: /Wallet/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WalletManagementViewModel formModel)
        {
            int userId = GetCurrentUserId();

            var result = await _walletService.CreateWalletAsync(
                userId,
                formModel.NewWalletName,
                formModel.NewInitialBalance,
                formModel.NewLimitAmount
            );

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index), new { walletId = formModel.SelectedWalletId });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { walletId = result.CreatedWalletId });
        }

        // =========================================
        // 3. THIẾT LẬP HẠN MỨC CHI TIÊU & CẬP NHẬT VÍ (Wallet Spending Limit)
        // POST: /Wallet/UpdateSettings
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(WalletManagementViewModel formModel)
        {
            int userId = GetCurrentUserId();

            var result = await _walletService.UpdateWalletSettingsAsync(
                userId,
                formModel.EditWalletId,
                formModel.EditWalletName,
                formModel.EditLimitAmount
            );

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new { walletId = formModel.EditWalletId });
        }
    }
}

