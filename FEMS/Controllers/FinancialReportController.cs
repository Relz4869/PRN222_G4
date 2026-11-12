
using FEMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FEMS.Controllers
{
    [Authorize]
    public class FinancialReportController : Controller
    {
        private readonly IFinancialReportService _reportService;

        public FinancialReportController(
            IFinancialReportService reportService)
        {
            _reportService = reportService;
        }


        [HttpGet]
        public async Task<IActionResult> Index(
    int months = 1, string? selectedMonth = null)
        {
            string? userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _reportService.GetSummaryAsync(
                userId, months, selectedMonth);

            if (model == null)
            {
                return RedirectToAction("Setup", "Household");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(
    int months = 1,
    string? selectedMonth = null,
    int? selectedMemberId = null)
        {
            string? userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _reportService.GetDetailedReportAsync(
        userId, months, selectedMonth, selectedMemberId);

            if (model == null)
            {
                return RedirectToAction("Setup", "Household");
            }

            return View(model);
        }

    }
}
