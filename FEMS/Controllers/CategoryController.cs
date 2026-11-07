using System.Security.Claims;
using FEMS.Models.Entities;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: /Category/Index
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, int? editId, int page = 1)
        {
            // 1. Lấy UserID từ phiên đăng nhập
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var member = await _categoryService.GetMemberByUserIdAsync(userId);

            var model = new CategoryViewModel();

            if (member != null)
            {
                model.HouseholdMemberId = member.HouseholdMemberId;
                model.Categories = await _categoryService.GetCategoriesAsync(member.HouseholdMemberId, search, status);

                // Nếu người dùng bấm nút "Sửa", đổ dữ liệu danh mục đó vào form bên trái
                if (editId.HasValue)
                {
                    var editCat = await _categoryService.GetByIdAsync(editId.Value);
                    if (editCat != null)
                    {
                        model.CategoryId = editCat.CategoryId;
                        model.CategoryName = editCat.CategoryName;
                        model.Description = editCat.Description;
                        model.Status = editCat.Status;
                    }
                }
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            return View("Category", model);
        }

        // POST: /Category/Save (Xử lý cả Thêm mới và Cập nhật)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(CategoryViewModel model)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var member = await _categoryService.GetMemberByUserIdAsync(userId);

            if (member == null)
            {
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                model.HouseholdMemberId = member.HouseholdMemberId;
                model.Categories = await _categoryService.GetCategoriesAsync(member.HouseholdMemberId, null, null);
                return View("Category", model);
            }

            var category = new Category
            {
                CategoryId = model.CategoryId,
                CategoryName = model.CategoryName,
                Description = model.Description,
                Status = model.Status,
                HouseholdMemberId = member.HouseholdMemberId
            };

            bool isSuccess = await _categoryService.SaveCategoryAsync(category);

            if (!isSuccess)
            {
                ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại!");
                model.HouseholdMemberId = member.HouseholdMemberId;
                model.Categories = await _categoryService.GetCategoriesAsync(member.HouseholdMemberId, null, null);
                return View("Category", model);
            }

            return RedirectToAction("Index");
        }

        // POST: /Category/ToggleStatus (Xử lý nút Tắt / Khôi phục)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string status, int page = 1, string? search = null, string? statusFilter = null)
        {
            await _categoryService.ChangeStatusAsync(id, status);
            return RedirectToAction("Index", new { page, search, status = statusFilter });
        }
    }
}
