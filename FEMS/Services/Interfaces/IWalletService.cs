using FEMS.Models.Entities;
using FEMS.ViewModels.Wallet;

namespace FEMS.Services.Interfaces
{
    public interface IWalletService
    {
        // Lấy thông tin thành viên theo UserID
        Task<HouseholdMember?> GetMemberByUserIdAsync(int userId);

        // Lấy dữ liệu tổng hợp cho màn hình Quản lý Ví cá nhân (Danh sách ví + Ví đang chọn + Lịch sử biến động)
        Task<WalletManagementViewModel> GetWalletManagementDataAsync(int userId, int? selectedWalletId = null);

        // UC: Create Personal Wallet (Tạo ví cá nhân mới)
        Task<(bool Success, string Message, int? CreatedWalletId)> CreateWalletAsync(
            int userId,
            string walletName,
            decimal initialBalance,
            decimal? limitAmount);

        // UC: Wallet Spending Limit & Update Wallet (Thiết lập / Cập nhật tên ví & hạn mức chi tiêu tháng)
        Task<(bool Success, string Message)> UpdateWalletSettingsAsync(
            int userId,
            int walletId,
            string walletName,
            decimal? limitAmount);
    }
}

