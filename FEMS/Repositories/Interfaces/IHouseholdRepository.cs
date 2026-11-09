using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface IHouseholdRepository
    {
        // Lấy thông tin thành viên đang Active của một User (kèm Household và User)
        Task<HouseholdMember?> GetActiveMemberByUserIdAsync(int userId);

        // Lấy thông tin thành viên theo HouseholdMemberId (kèm User và Household)
        Task<HouseholdMember?> GetMemberByIdAsync(int householdMemberId);

        // Tìm bản ghi thành viên theo cặp (HouseholdId, UserId) bất kể Active hay Inactive
        Task<HouseholdMember?> GetMemberByHouseholdAndUserAsync(int householdId, int userId);

        // Lấy thông tin Household theo ID
        Task<Household?> GetHouseholdByIdAsync(int householdId);

        // Lấy danh sách thành viên đang Active trong một hộ gia đình (kèm thông tin User)
        Task<List<HouseholdMember>> GetActiveMembersByHouseholdIdAsync(int householdId);

        // Tạo mới một Household
        Task<Household> AddHouseholdAsync(Household household);

        // Cập nhật thông tin một Household (ví dụ: giải tán/xóa hộ gia đình)
        Task UpdateHouseholdAsync(Household household);

        // Thêm mới một HouseholdMember
        Task AddMemberAsync(HouseholdMember member);

        // Cập nhật thông tin một HouseholdMember
        Task UpdateMemberAsync(HouseholdMember member);

        // Cập nhật nhiều HouseholdMember cùng lúc
        Task UpdateMembersAsync(IEnumerable<HouseholdMember> members);

        // Tạo thông báo mới trong bảng Notification
        Task AddNotificationAsync(Notification notification);

        // Lấy danh sách các thông báo của một User (mới nhất trước)
        Task<List<Notification>> GetNotificationsByUserIdAsync(int userId);

        // Lấy danh sách lời mời đang chờ (chưa đọc) của một Hộ gia đình (kèm thông tin User được mời)
        Task<List<Notification>> GetUnreadInviteNotificationsByHouseholdIdAsync(int householdId);

        // Lấy một thông báo theo ID (kèm thông tin User)
        Task<Notification?> GetNotificationByIdAsync(int notificationId);

        // Cập nhật thông báo (ví dụ: đánh dấu đã đọc)
        Task UpdateNotificationAsync(Notification notification);

        // Xóa một thông báo (ví dụ: khi Manager hủy lời mời)
        Task DeleteNotificationAsync(Notification notification);

        // Lấy danh sách ví cá nhân của một thành viên (UC-12)
        Task<List<Wallet>> GetWalletsByMemberIdAsync(int householdMemberId);

        // Lấy danh sách các khoản thu (Income) của một thành viên (kèm Wallet)
        Task<List<Income>> GetIncomesByMemberIdAsync(int householdMemberId);

        // Lấy danh sách các khoản chi (Expense) của một thành viên (kèm Wallet và Category)
        Task<List<Expense>> GetExpensesByMemberIdAsync(int householdMemberId);
    }
}

