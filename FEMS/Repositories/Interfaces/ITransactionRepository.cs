using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        // Lấy thông tin thành viên theo UserID đang đăng nhập (ưu tiên thành viên đang Active)
        Task<HouseholdMember?> GetMemberByUserIdAsync(int userId);

        // Lấy danh sách ví cá nhân của thành viên
        Task<List<Wallet>> GetWalletsByMemberIdAsync(int memberId);

        // Tìm 1 ví theo ID
        Task<Wallet?> GetWalletByIdAsync(int walletId);

        // Cập nhật số dư ví
        Task UpdateWalletAsync(Wallet wallet);

        // Lấy danh sách danh mục đang hoạt động (Active) của thành viên
        Task<List<Category>> GetActiveCategoriesByMemberIdAsync(int memberId);

        // Tìm 1 danh mục theo ID
        Task<Category?> GetCategoryByIdAsync(int categoryId);

        // Lấy danh sách khoản thu của thành viên
        Task<List<Income>> GetIncomesByMemberIdAsync(int memberId);

        // Lấy danh sách khoản chi của thành viên (kèm thông tin Ví và Danh mục)
        Task<List<Expense>> GetExpensesByMemberIdAsync(int memberId);

        // Thêm khoản thu mới
        Task<Income> AddIncomeAsync(Income income);

        // Thêm khoản chi mới
        Task<Expense> AddExpenseAsync(Expense expense);

        // Lấy danh sách Quản lý hộ (Manager) đang Active trong cùng hộ gia đình để gửi thông báo
        Task<List<HouseholdMember>> GetActiveManagersByHouseholdIdAsync(int householdId);

        // Thêm thông báo in-app
        Task AddNotificationAsync(Notification notification);
    }
}

