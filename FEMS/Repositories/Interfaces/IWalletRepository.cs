using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface IWalletRepository
    {
        // Lấy thông tin thành viên theo UserID đang đăng nhập (ưu tiên thành viên đang Active)
        Task<HouseholdMember?> GetMemberByUserIdAsync(int userId);

        // Lấy danh sách ví cá nhân của thành viên
        Task<List<Wallet>> GetWalletsByMemberIdAsync(int memberId);

        // Tìm 1 ví theo ID
        Task<Wallet?> GetByIdAsync(int walletId);

        // Thêm ví cá nhân mới
        Task<Wallet> AddAsync(Wallet wallet);

        // Cập nhật thông tin ví (số dư, hạn mức chi tiêu, tên ví)
        Task UpdateAsync(Wallet wallet);

        // Xóa ví cá nhân (chỉ khi chưa có giao dịch phát sinh)
        Task DeleteAsync(Wallet wallet);

        // Lấy danh sách khoản thu của thành viên
        Task<List<Income>> GetIncomesByMemberIdAsync(int memberId);

        // Lấy danh sách khoản chi của thành viên (kèm thông tin Danh mục)
        Task<List<Expense>> GetExpensesByMemberIdAsync(int memberId);

        // Thêm thông báo in-app (khi vượt hạn mức chi tiêu hoặc số dư âm)
        Task AddNotificationAsync(Notification notification);
    }
}

