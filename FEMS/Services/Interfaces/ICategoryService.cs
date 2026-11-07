using FEMS.Models.Entities;

namespace FEMS.Services.Interfaces
{
    public interface ICategoryService
    {
        // Lấy thông tin thành viên theo UserID
        Task<HouseholdMember?> GetMemberByUserIdAsync(int userId);

        // Lấy danh sách danh mục (có hỗ trợ tìm kiếm và lọc trạng thái)
        Task<List<Category>> GetCategoriesAsync(int memberId, string? search, string? status);

        // Lấy 1 danh mục theo ID để đổ lên form sửa
        Task<Category?> GetByIdAsync(int id);

        // Lưu danh mục (Thêm mới hoặc Cập nhật)
        Task<bool> SaveCategoryAsync(Category category);

        // Đổi trạng thái Active / Inactive
        Task ChangeStatusAsync(int id, string status);
    }
}
