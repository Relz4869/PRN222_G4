using FEMS.Data;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly FemsDbContext _context;

        public CategoryRepository(FemsDbContext context)
        {
            _context = context;
        }

        // Lấy thông tin thành viên theo UserID đang đăng nhập
        public async Task<HouseholdMember?> GetMemberByUserIdAsync(int userId)
        {
            return await _context.Set<HouseholdMember>()
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        // Lấy danh sách danh mục của thành viên đó
        public async Task<List<Category>> GetAllByMemberIdAsync(int memberId)
        {
            return await _context.Set<Category>()
                .Where(c => c.HouseholdMemberId == memberId)
                .ToListAsync();
        }

        // Tìm 1 danh mục theo ID
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Set<Category>()
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        // Thêm danh mục mới
        public async Task AddAsync(Category category)
        {
            await _context.Set<Category>().AddAsync(category);
            await _context.SaveChangesAsync();
        }

        // Cập nhật danh mục
        public async Task UpdateAsync(Category category)
        {
            _context.Set<Category>().Update(category);
            await _context.SaveChangesAsync();
        }
    }
}
