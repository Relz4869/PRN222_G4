using FEMS.ViewModels.Transaction;

namespace FEMS.Services.Interfaces
{
    public interface ITransactionService
    {
        // Lấy dữ liệu màn hình Quản lý Giao dịch (Danh sách Ví, Danh mục Active, Lịch sử giao dịch cá nhân có lọc)
        Task<TransactionManagementViewModel> GetTransactionManagementDataAsync(
            int userId,
            int? filterWalletId = null,
            string? filterMonth = null);

        // UC: Create Transaction (Ghi nhận Thu nhập hoặc Chi tiêu mới, cập nhật số dư ví & kích hoạt cảnh báo)
        Task<(bool Success, string Message)> CreateTransactionAsync(
            int userId,
            TransactionManagementViewModel input);
    }
}

