using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;

namespace FEMS.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // Lấy thông tin thành viên theo UserID
        public async Task<HouseholdMember?> GetMemberByUserIdAsync(int userId)
        {
            return await _categoryRepository.GetMemberByUserIdAsync(userId);
        }

        // Lấy danh sách danh mục (có tìm kiếm và lọc trạng thái)
        public async Task<List<Category>> GetCategoriesAsync(int memberId, string? search, string? status)
        {
            var list = await _categoryRepository.GetAllByMemberIdAsync(memberId);

            // Lọc theo trạng thái nếu có chọn
            if (!string.IsNullOrEmpty(status))
            {
                list = list.Where(c => c.Status == status).ToList();
            }

            // Tìm kiếm theo tên hoặc mô tả nếu có nhập
            if (!string.IsNullOrEmpty(search))
            {
                string keyword = search.Trim().ToLower();
                list = list.Where(c =>
                    c.CategoryName.ToLower().Contains(keyword) ||
                    (c.Description != null && c.Description.ToLower().Contains(keyword))
                ).ToList();
            }

            return list;
        }

        // Lấy 1 danh mục theo ID
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        // Lưu danh mục (Thêm mới hoặc Cập nhật)
        public async Task<bool> SaveCategoryAsync(Category category)
        {
            var allCategories = await _categoryRepository.GetAllByMemberIdAsync(category.HouseholdMemberId);

            // Kiểm tra trùng tên danh mục của cùng 1 thành viên
            bool isDuplicate = allCategories.Any(c =>
                c.CategoryName.ToLower() == category.CategoryName.Trim().ToLower() &&
                c.CategoryId != category.CategoryId);

            if (isDuplicate)
            {
                return false;
            }

            // Nếu CategoryId == 0 thì là Thêm mới
            if (category.CategoryId == 0)
            {
                category.CategoryName = category.CategoryName.Trim();
                await _categoryRepository.AddAsync(category);
            }
            else
            {
                // Ngược lại là Cập nhật
                var existing = await _categoryRepository.GetByIdAsync(category.CategoryId);
                if (existing != null)
                {
                    existing.CategoryName = category.CategoryName.Trim();
                    existing.Description = category.Description;
                    existing.Status = category.Status;
                    await _categoryRepository.UpdateAsync(existing);
                }
            }

            return true;
        }

        // Đổi trạng thái Tắt (Inactive) hoặc Khôi phục (Active)
        public async Task ChangeStatusAsync(int id, string status)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category != null)
            {
                category.Status = status;
                await _categoryRepository.UpdateAsync(category);
            }
        }
    }
}
