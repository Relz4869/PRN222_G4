using FEMS.Models.Entities;
using FEMS.ViewModels.Household;

namespace FEMS.Services.Interfaces
{
    public interface IHouseholdService
    {
        // Kiểm tra xem User đã có HouseholdMember đang Active hay chưa
        Task<HouseholdMember?> GetActiveMembershipAsync(int userId);

        // Lấy dữ liệu tổng quan Hộ gia đình cho trang quản lý
        Task<HouseholdDashboardViewModel?> GetDashboardAsync(int userId);

        // UC-05: Tạo hộ gia đình mới (User tạo hộ sẽ trở thành Manager)
        Task<(bool Success, string Message)> CreateHouseholdAsync(int userId, string householdName);

        // UC-07 (Phần 1): Manager mời thành viên đã đăng ký qua Email -> sinh mã mời lưu vào Notification
        Task<(bool Success, string Message, string? InviteCode)> InviteMemberByEmailAsync(int managerUserId, string targetEmail);

        // Hủy / Xóa một lời mời đang chờ (dành cho Manager)
        Task<(bool Success, string Message)> CancelInviteAsync(int managerUserId, int notificationId);

        // Lấy danh sách lời mời đang chờ của User
        Task<List<PendingInviteItemViewModel>> GetPendingInvitesAsync(int userId);

        // UC-07 (Phần 2): Member nhập mã mời để tham gia hộ gia đình
        Task<(bool Success, string Message)> JoinHouseholdByCodeAsync(int userId, string inviteCode);

        // UC-08: Manager thăng cấp một Member lên vai trò Manager
        Task<(bool Success, string Message)> PromoteMemberAsync(int managerUserId, int targetHouseholdMemberId);

        // Người tạo hộ (Creator / Chủ hộ) gỡ quyền Manager của thành viên xuống lại Member
        Task<(bool Success, string Message)> DemoteMemberAsync(int creatorUserId, int targetHouseholdMemberId);

        // UC-09: Manager xóa một Member (hoặc Người tạo hộ xóa bất kỳ thành viên nào) khỏi hộ gia đình
        Task<(bool Success, string Message)> RemoveMemberAsync(int managerUserId, int targetHouseholdMemberId);

        // Thành viên tham gia hộ tự rời khỏi hộ gia đình (giữ nguyên tài khoản, ví cá nhân và giao dịch)
        Task<(bool Success, string Message)> LeaveHouseholdAsync(int userId);

        // Người tạo hộ (Chủ hộ) xóa / giải tán hộ gia đình
        Task<(bool Success, string Message)> DeleteHouseholdAsync(int creatorUserId);

        // UC-29 (BR-37): Manager thiết lập hạn mức cảnh báo giao dịch (AlertAmount) cho thành viên
        Task<(bool Success, string Message)> SetMemberAlertThresholdAsync(
            int managerUserId,
            int targetHouseholdMemberId,
            decimal? alertAmount);

        // UC-12 & UC-16 (BR-16, BR-20): Manager xem chi tiết Ví cá nhân & Lịch sử giao dịch của thành viên ở chế độ Read-only
        Task<(bool Success, string Message, MemberFinanceDetailViewModel? Data)> GetMemberFinanceDetailAsync(
            int managerUserId,
            int targetHouseholdMemberId,
            int? walletId = null,
            string? transactionType = null,
            string? search = null,
            int page = 1,
            int pageSize = 7);
    }
}

