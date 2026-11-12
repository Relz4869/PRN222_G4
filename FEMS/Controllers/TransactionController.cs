using System.Security.Claims;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        private int GetCurrentUserId()
        {
            string? val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(val, out int id) ? id : 0;
        }

        // =========================================
        // 1. HIỂN THỊ TRANG GHI NHẬN & XEM LỊCH SỬ GIAO DỊCH CÁ NHÂN (View Own Transactions)
        // GET: /Transaction/Index
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Index(int? filterWalletId = null, string? filterMonth = null, int page = 1)
        {
            int userId = GetCurrentUserId();
            var model = await _transactionService.GetTransactionManagementDataAsync(userId, filterWalletId, filterMonth);

            if (!model.HasHouseholdMembership)
            {
                TempData["ErrorMessage"] = "Vui lòng tạo hoặc tham gia một Hộ gia đình trước khi ghi nhận giao dịch.";
                return RedirectToAction("Setup", "Household");
            }

            ViewBag.CurrentPage = page;
            return View(model);
        }

        // =========================================
        // 2. GHI NHẬN GIAO DỊCH MỚI (Create Transaction)
        // POST: /Transaction/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionManagementViewModel formModel)
        {
            int userId = GetCurrentUserId();
            var result = await _transactionService.CreateTransactionAsync(userId, formModel);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                filterWalletId = formModel.FilterWalletId,
                filterMonth = formModel.FilterMonth
            });
        }
    }
}

