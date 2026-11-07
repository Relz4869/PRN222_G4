using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        // Lấy thông tin thành viên theo UserID đang đăng nhập
        Task<HouseholdMember?> GetMemberByUserIdAsync(int userId);

        // Lấy danh sách danh mục của thành viên đó
        Task<List<Category>> GetAllByMemberIdAsync(int memberId);

        // Tìm 1 danh mục theo ID
        Task<Category?> GetByIdAsync(int id);

        // Thêm danh mục mới
        Task AddAsync(Category category);

        // Cập nhật danh mục
        Task UpdateAsync(Category category);
    }
}
