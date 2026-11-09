using System.Text.RegularExpressions;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Household;

namespace FEMS.Services.Implementations
{
    public class HouseholdService : IHouseholdService
    {
        private readonly IHouseholdRepository _householdRepository;
        private readonly IUserRepository _userRepository;

        // Định dạng lưu mã mời trong bảng Notification:
        // [INVITE:{HouseholdId}:{InviteCode}] Bạn được mời tham gia hộ gia đình "{HouseholdName}". Mã tham gia của bạn là: {InviteCode}
        private static readonly Regex InviteMessageRegex = new(
            @"^\[INVITE:(?<hid>\d+):(?<code>[A-Z0-9\-]+)\]\s*(?<text>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public HouseholdService(
            IHouseholdRepository householdRepository,
            IUserRepository userRepository)
        {
            _householdRepository = householdRepository;
            _userRepository = userRepository;
        }

        public async Task<HouseholdMember?> GetActiveMembershipAsync(int userId)
        {
            return await _householdRepository.GetActiveMemberByUserIdAsync(userId);
        }

        public async Task<HouseholdDashboardViewModel?> GetDashboardAsync(int userId)
        {
            var currentMember = await _householdRepository.GetActiveMemberByUserIdAsync(userId);
            if (currentMember == null)
            {
                return null;
            }

            var members = await _householdRepository.GetActiveMembersByHouseholdIdAsync(currentMember.HouseholdId);
            var creatorMember = FindCreatorMember(members);
            int creatorUserId = creatorMember?.UserId ?? 0;
            int creatorMemberId = creatorMember?.HouseholdMemberId ?? 0;

            var sentInvites = new List<SentInviteItemViewModel>();

            if (string.Equals(currentMember.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                var inviteNotifications = await _householdRepository
                    .GetUnreadInviteNotificationsByHouseholdIdAsync(currentMember.HouseholdId);

                foreach (var n in inviteNotifications)
                {
                    var match = InviteMessageRegex.Match(n.Message);
                    if (!match.Success)
                    {
                        continue;
                    }

                    sentInvites.Add(new SentInviteItemViewModel
                    {
                        NotificationId = n.NotificationId,
                        InvitedUserId = n.UserId,
                        InvitedFullName = n.User.FullName,
                        InvitedEmail = n.User.Email,
                        InviteCode = match.Groups["code"].Value.ToUpperInvariant(),
                        CreatedAt = n.CreatedAt
                    });
                }
            }

            return new HouseholdDashboardViewModel
            {
                HouseholdId = currentMember.Household.HouseholdId,
                HouseholdName = currentMember.Household.HouseholdName,
                CreatedAt = currentMember.Household.CreatedAt,
                CurrentUserId = userId,
                CurrentMemberId = currentMember.HouseholdMemberId,
                CurrentUserRole = currentMember.Role,
                CreatorMemberId = creatorMemberId,
                CreatorUserId = creatorUserId,
                SentInvites = sentInvites,
                Members = members.Select(m => new HouseholdMemberItemViewModel
                {
                    HouseholdMemberId = m.HouseholdMemberId,
                    UserId = m.UserId,
                    FullName = m.User.FullName,
                    Email = m.User.Email,
                    Phone = m.User.Phone,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    AlertAmount = m.AlertAmount,
                    Status = m.Status,
                    IsCreator = m.UserId == creatorUserId
                }).ToList()
            };
        }

        private static HouseholdMember? FindCreatorMember(List<HouseholdMember> activeMembers)
        {
            return activeMembers
                .Where(m => string.Equals(m.Role, "Manager", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.JoinedAt)
                .ThenBy(m => m.HouseholdMemberId)
                .FirstOrDefault()
                ?? activeMembers.OrderBy(m => m.JoinedAt).ThenBy(m => m.HouseholdMemberId).FirstOrDefault();
        }

        // =========================================
        // UC-05: CREATE HOUSEHOLD (BR-03, BR-04)
        // =========================================
        public async Task<(bool Success, string Message)> CreateHouseholdAsync(int userId, string householdName)
        {
            if (string.IsNullOrWhiteSpace(householdName))
            {
                return (false, "Tên hộ gia đình không được để trống.");
            }

            // BR-04: Mỗi tài khoản chỉ thuộc 1 hộ gia đình tại 1 thời điểm
            var existingActive = await _householdRepository.GetActiveMemberByUserIdAsync(userId);
            if (existingActive != null)
            {
                return (false, "Bạn đã là thành viên của một hộ gia đình.");
            }

            var newHousehold = new Household
            {
                HouseholdName = householdName.Trim(),
                CreatedAt = DateTime.Now,
                Status = "Active"
            };

            var savedHousehold = await _householdRepository.AddHouseholdAsync(newHousehold);

            // BR-03: Người tạo hộ gia đình được gán quyền Manager
            var managerMember = new HouseholdMember
            {
                HouseholdId = savedHousehold.HouseholdId,
                UserId = userId,
                Role = "Manager",
                JoinedAt = DateTime.Now,
                Status = "Active"
            };

            await _householdRepository.AddMemberAsync(managerMember);

            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = userId,
                Message = $"Chúc mừng! Bạn đã tạo hộ gia đình \"{savedHousehold.HouseholdName}\" với vai trò Quản lý (Manager).",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Tạo hộ gia đình \"{savedHousehold.HouseholdName}\" thành công!");
        }

        // =========================================
        // UC-07: INVITE MEMBER BY EMAIL (BR-06)
        // =========================================
        public async Task<(bool Success, string Message, string? InviteCode)> InviteMemberByEmailAsync(
            int managerUserId,
            string targetEmail)
        {
            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                return (false, "Vui lòng nhập email của người dùng cần mời.", null);
            }

            var manager = await _householdRepository.GetActiveMemberByUserIdAsync(managerUserId);
            if (manager == null || !string.Equals(manager.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ Quản lý (Manager) mới có quyền mời thành viên.", null);
            }

            // BR-06: Chỉ mời được người dùng đã đăng ký tài khoản FEMS
            var invitedUser = await _userRepository.GetByEmailAsync(targetEmail.Trim());
            if (invitedUser == null)
            {
                return (false, "Không tìm thấy tài khoản FEMS nào đăng ký với email này.", null);
            }

            if (invitedUser.UserId == managerUserId)
            {
                return (false, "Bạn đang ở trong hộ gia đình này rồi.", null);
            }

            // BR-04: Kiểm tra người được mời đã thuộc hộ gia đình Active nào chưa
            var invitedUserActiveMembership = await _householdRepository.GetActiveMemberByUserIdAsync(invitedUser.UserId);
            if (invitedUserActiveMembership != null)
            {
                if (invitedUserActiveMembership.HouseholdId == manager.HouseholdId)
                {
                    return (false, $"Thành viên {invitedUser.FullName} hiện đã ở trong hộ gia đình của bạn.", null);
                }

                return (false, $"Tài khoản {invitedUser.FullName} hiện đang thuộc một hộ gia đình khác.", null);
            }

            // Sinh mã mời 6 chữ số ngẫu nhiên gắn với HouseholdId
            string randomDigits = Random.Shared.Next(100000, 999999).ToString();
            string inviteCode = $"FEMS-{randomDigits}";

            // Nếu đã có lời mời cũ chưa đọc của hộ này cho user này, xóa lời mời cũ để thay bằng mã mới
            var existingInvites = await _householdRepository
                .GetUnreadInviteNotificationsByHouseholdIdAsync(manager.HouseholdId);
            foreach (var oldInvite in existingInvites.Where(x => x.UserId == invitedUser.UserId))
            {
                await _householdRepository.DeleteNotificationAsync(oldInvite);
            }

            string notificationMsg =
                $"[INVITE:{manager.HouseholdId}:{inviteCode}] Bạn được {manager.User.FullName} mời tham gia hộ gia đình \"{manager.Household.HouseholdName}\". Mã tham gia của bạn là: {inviteCode}";

            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = invitedUser.UserId,
                Message = notificationMsg,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (
                true,
                $"Đã gửi lời mời đến {invitedUser.FullName} ({invitedUser.Email}). Mã tham gia hộ là: {inviteCode}",
                inviteCode
            );
        }

        // =========================================
        // CANCEL / REVOKE INVITE (MANAGER)
        // =========================================
        public async Task<(bool Success, string Message)> CancelInviteAsync(int managerUserId, int notificationId)
        {
            var manager = await _householdRepository.GetActiveMemberByUserIdAsync(managerUserId);
            if (manager == null || !string.Equals(manager.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ Quản lý (Manager) mới có quyền hủy lời mời.");
            }

            var notification = await _householdRepository.GetNotificationByIdAsync(notificationId);
            if (notification == null || notification.IsRead)
            {
                return (false, "Lời mời này không tồn tại hoặc đã được sử dụng.");
            }

            var match = InviteMessageRegex.Match(notification.Message);
            if (!match.Success ||
                !int.TryParse(match.Groups["hid"].Value, out int inviteHouseholdId) ||
                inviteHouseholdId != manager.HouseholdId)
            {
                return (false, "Bạn không có quyền hủy lời mời này.");
            }

            string invitedName = notification.User?.FullName ?? "người dùng";
            string invitedEmail = notification.User?.Email ?? "";

            await _householdRepository.DeleteNotificationAsync(notification);

            return (true, $"Đã hủy lời mời gửi tới {invitedName} ({invitedEmail}). Mã mời này không còn hiệu lực.");
        }

        // =========================================
        // GET PENDING INVITES FOR WAITING PAGE
        // =========================================
        public async Task<List<PendingInviteItemViewModel>> GetPendingInvitesAsync(int userId)
        {
            var notifications = await _householdRepository.GetNotificationsByUserIdAsync(userId);
            var list = new List<PendingInviteItemViewModel>();

            foreach (var n in notifications.Where(x => !x.IsRead))
            {
                var match = InviteMessageRegex.Match(n.Message);
                if (!match.Success)
                {
                    continue;
                }

                if (!int.TryParse(match.Groups["hid"].Value, out int householdId))
                {
                    continue;
                }

                var household = await _householdRepository.GetHouseholdByIdAsync(householdId);
                if (household == null)
                {
                    continue;
                }

                list.Add(new PendingInviteItemViewModel
                {
                    NotificationId = n.NotificationId,
                    HouseholdId = householdId,
                    HouseholdName = household.HouseholdName,
                    InviteCode = match.Groups["code"].Value.ToUpperInvariant(),
                    Message = match.Groups["text"].Value,
                    CreatedAt = n.CreatedAt
                });
            }

            return list;
        }

        // =========================================
        // UC-07: JOIN HOUSEHOLD BY CODE
        // =========================================
        public async Task<(bool Success, string Message)> JoinHouseholdByCodeAsync(int userId, string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                return (false, "Vui lòng nhập mã tham gia hộ gia đình.");
            }

            var existingActive = await _householdRepository.GetActiveMemberByUserIdAsync(userId);
            if (existingActive != null)
            {
                return (false, "Bạn đã là thành viên của một hộ gia đình.");
            }

            string normalizedInput = inviteCode.Trim().ToUpperInvariant();

            var notifications = await _householdRepository.GetNotificationsByUserIdAsync(userId);
            Notification? matchedNotification = null;
            int targetHouseholdId = 0;

            foreach (var n in notifications.Where(x => !x.IsRead))
            {
                var match = InviteMessageRegex.Match(n.Message);
                if (!match.Success)
                {
                    continue;
                }

                string codeInMsg = match.Groups["code"].Value.Trim().ToUpperInvariant();
                // Cho phép nhập đầy đủ "FEMS-123456" hoặc chỉ 6 số "123456"
                string digitsOnly = codeInMsg.StartsWith("FEMS-")
                    ? codeInMsg.Substring(5)
                    : codeInMsg;

                if (string.Equals(normalizedInput, codeInMsg, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedInput, digitsOnly, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(match.Groups["hid"].Value, out targetHouseholdId))
                    {
                        matchedNotification = n;
                        break;
                    }
                }
            }

            if (matchedNotification == null || targetHouseholdId <= 0)
            {
                return (false, "Mã tham gia không chính xác hoặc bạn chưa nhận được lời mời cho mã này.");
            }

            var household = await _householdRepository.GetHouseholdByIdAsync(targetHouseholdId);
            if (household == null)
            {
                return (false, "Hộ gia đình này không còn hoạt động.");
            }

            // Kiểm tra xem User đã từng có bản ghi (Inactive) trong hộ này chưa để tránh lỗi UQ_HouseholdMember
            var existingRecord = await _householdRepository.GetMemberByHouseholdAndUserAsync(targetHouseholdId, userId);
            if (existingRecord != null)
            {
                existingRecord.Status = "Active";
                existingRecord.Role = "Member";
                existingRecord.JoinedAt = DateTime.Now;
                await _householdRepository.UpdateMemberAsync(existingRecord);
            }
            else
            {
                var newMember = new HouseholdMember
                {
                    HouseholdId = targetHouseholdId,
                    UserId = userId,
                    Role = "Member",
                    JoinedAt = DateTime.Now,
                    Status = "Active"
                };
                await _householdRepository.AddMemberAsync(newMember);
            }

            // Đánh dấu các lời mời chưa đọc của user là đã đọc
            matchedNotification.IsRead = true;
            await _householdRepository.UpdateNotificationAsync(matchedNotification);

            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = userId,
                Message = $"Chào mừng bạn đã gia nhập hộ gia đình \"{household.HouseholdName}\"!",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Tham gia hộ gia đình \"{household.HouseholdName}\" thành công!");
        }

        // =========================================
        // UC-08: PROMOTE ROLE (BR-10, BR-42)
        // =========================================
        public async Task<(bool Success, string Message)> PromoteMemberAsync(
            int managerUserId,
            int targetHouseholdMemberId)
        {
            var manager = await _householdRepository.GetActiveMemberByUserIdAsync(managerUserId);
            if (manager == null || !string.Equals(manager.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ Quản lý (Manager) mới có quyền thăng cấp thành viên.");
            }

            var targetMember = await _householdRepository.GetMemberByIdAsync(targetHouseholdMemberId);
            if (targetMember == null ||
                targetMember.HouseholdId != manager.HouseholdId ||
                targetMember.Status != "Active")
            {
                return (false, "Không tìm thấy thành viên hợp lệ trong hộ gia đình.");
            }

            if (string.Equals(targetMember.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Thành viên {targetMember.User.FullName} đã có vai trò Quản lý (Manager).");
            }

            targetMember.Role = "Manager";
            await _householdRepository.UpdateMemberAsync(targetMember);

            // BR-42: Gửi thông báo in-app khi thay đổi vai trò
            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = targetMember.UserId,
                Message = $"Vai trò của bạn trong hộ gia đình \"{manager.Household.HouseholdName}\" đã được thăng cấp thành Quản lý (Manager).",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Đã thăng cấp {targetMember.User.FullName} lên vai trò Quản lý (Manager).");
        }

        // =========================================
        // DEMOTE ROLE (CREATOR / CHỦ HỘ GỠ QUYỀN MANAGER)
        // =========================================
        public async Task<(bool Success, string Message)> DemoteMemberAsync(
            int creatorUserId,
            int targetHouseholdMemberId)
        {
            var currentMember = await _householdRepository.GetActiveMemberByUserIdAsync(creatorUserId);
            if (currentMember == null)
            {
                return (false, "Bạn hiện không thuộc hộ gia đình nào.");
            }

            var activeMembers = await _householdRepository.GetActiveMembersByHouseholdIdAsync(currentMember.HouseholdId);
            var creator = FindCreatorMember(activeMembers);

            if (creator == null || creator.UserId != creatorUserId)
            {
                return (false, "Chỉ người tạo hộ gia đình (Chủ hộ) mới có quyền gỡ quyền Quản lý (Manager).");
            }

            var targetMember = activeMembers.FirstOrDefault(m => m.HouseholdMemberId == targetHouseholdMemberId);
            if (targetMember == null)
            {
                return (false, "Không tìm thấy thành viên hợp lệ trong hộ gia đình.");
            }

            if (targetMember.UserId == creatorUserId)
            {
                return (false, "Người tạo hộ gia đình không thể tự gỡ quyền của chính mình.");
            }

            if (!string.Equals(targetMember.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Thành viên {targetMember.User.FullName} hiện đang là Thành viên (Member).");
            }

            targetMember.Role = "Member";
            await _householdRepository.UpdateMemberAsync(targetMember);

            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = targetMember.UserId,
                Message = $"Vai trò của bạn trong hộ gia đình \"{currentMember.Household.HouseholdName}\" đã được chuyển về Thành viên (Member).",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Đã gỡ quyền Quản lý của {targetMember.User.FullName} (chuyển về Thành viên).");
        }

        // =========================================
        // UC-09: REMOVE MEMBER (BR-10, BR-11, BR-42 + CREATOR PRIVILEGE)
        // =========================================
        public async Task<(bool Success, string Message)> RemoveMemberAsync(
            int managerUserId,
            int targetHouseholdMemberId)
        {
            var manager = await _householdRepository.GetActiveMemberByUserIdAsync(managerUserId);
            if (manager == null || !string.Equals(manager.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ Quản lý (Manager) mới có quyền xóa thành viên khỏi hộ gia đình.");
            }

            var activeMembers = await _householdRepository.GetActiveMembersByHouseholdIdAsync(manager.HouseholdId);
            var creator = FindCreatorMember(activeMembers);
            bool isCreator = creator != null && creator.UserId == managerUserId;

            var targetMember = activeMembers.FirstOrDefault(m => m.HouseholdMemberId == targetHouseholdMemberId);
            if (targetMember == null)
            {
                return (false, "Không tìm thấy thành viên hợp lệ trong hộ gia đình.");
            }

            if (targetMember.UserId == managerUserId)
            {
                return (false, "Bạn không thể tự xóa chính mình bằng nút này.");
            }

            if (creator != null && targetMember.UserId == creator.UserId)
            {
                return (false, "Không thể xóa Người tạo hộ gia đình (Chủ hộ).");
            }

            // Nếu đối tượng bị xóa là Manager: chỉ Người tạo hộ (Chủ hộ) mới được phép xóa
            if (string.Equals(targetMember.Role, "Manager", StringComparison.OrdinalIgnoreCase) && !isCreator)
            {
                return (false, "Chỉ Người tạo hộ gia đình (Chủ hộ) mới có quyền xóa một Quản lý (Manager) khác.");
            }

            // BR-11: Chuyển trạng thái sang Inactive để bảo toàn tài khoản, ví cá nhân, số dư và lịch sử giao dịch
            targetMember.Status = "Inactive";
            targetMember.Role = "Member";
            await _householdRepository.UpdateMemberAsync(targetMember);

            // BR-42: Gửi thông báo in-app cho thành viên bị xóa khỏi hộ
            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = targetMember.UserId,
                Message = $"Bạn đã không còn là thành viên của hộ gia đình \"{manager.Household.HouseholdName}\". Toàn bộ ví cá nhân và lịch sử giao dịch của bạn vẫn được giữ nguyên.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Đã xóa thành viên {targetMember.User.FullName} khỏi hộ gia đình (dữ liệu cá nhân của họ vẫn được bảo toàn).");
        }

        // =========================================
        // LEAVE HOUSEHOLD (THÀNH VIÊN TỰ RỜI HỘ)
        // =========================================
        public async Task<(bool Success, string Message)> LeaveHouseholdAsync(int userId)
        {
            var currentMember = await _householdRepository.GetActiveMemberByUserIdAsync(userId);
            if (currentMember == null)
            {
                return (false, "Bạn hiện không thuộc hộ gia đình nào.");
            }

            var activeMembers = await _householdRepository.GetActiveMembersByHouseholdIdAsync(currentMember.HouseholdId);
            var creator = FindCreatorMember(activeMembers);

            if (creator != null && creator.UserId == userId)
            {
                return (false, "Bạn là Người tạo hộ gia đình (Chủ hộ). Nếu muốn giải tán hộ gia đình, vui lòng sử dụng chức năng 'Xóa hộ gia đình'.");
            }

            string householdName = currentMember.Household.HouseholdName;

            currentMember.Status = "Inactive";
            currentMember.Role = "Member";
            await _householdRepository.UpdateMemberAsync(currentMember);

            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = userId,
                Message = $"Bạn đã rời khỏi hộ gia đình \"{householdName}\". Toàn bộ ví cá nhân và lịch sử giao dịch của bạn vẫn được giữ nguyên.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Bạn đã rời khỏi hộ gia đình \"{householdName}\" thành công.");
        }

        // =========================================
        // DELETE HOUSEHOLD (CHỦ HỘ XÓA / GIẢI TÁN HỘ GIA ĐÌNH)
        // =========================================
        public async Task<(bool Success, string Message)> DeleteHouseholdAsync(int creatorUserId)
        {
            var currentMember = await _householdRepository.GetActiveMemberByUserIdAsync(creatorUserId);
            if (currentMember == null)
            {
                return (false, "Bạn hiện không thuộc hộ gia đình nào.");
            }

            var activeMembers = await _householdRepository.GetActiveMembersByHouseholdIdAsync(currentMember.HouseholdId);
            var creator = FindCreatorMember(activeMembers);

            if (creator == null || creator.UserId != creatorUserId)
            {
                return (false, "Chỉ Người tạo hộ gia đình (Chủ hộ) mới có quyền xóa hộ gia đình.");
            }

            var household = await _householdRepository.GetHouseholdByIdAsync(currentMember.HouseholdId);
            if (household == null)
            {
                return (false, "Không tìm thấy hộ gia đình.");
            }

            string householdName = household.HouseholdName;

            // 1. Xóa các lời mời đang chờ xác nhận của hộ này
            var pendingInvites = await _householdRepository.GetUnreadInviteNotificationsByHouseholdIdAsync(household.HouseholdId);
            foreach (var invite in pendingInvites)
            {
                await _householdRepository.DeleteNotificationAsync(invite);
            }

            // 2. Chuyển tất cả các thành viên đang Active về Inactive để bảo toàn dữ liệu ví & giao dịch
            foreach (var m in activeMembers)
            {
                m.Status = "Inactive";
                if (m.UserId != creatorUserId)
                {
                    await _householdRepository.AddNotificationAsync(new Notification
                    {
                        UserId = m.UserId,
                        Message = $"Hộ gia đình \"{householdName}\" đã được Chủ hộ giải tán. Toàn bộ ví cá nhân và lịch sử giao dịch của bạn vẫn được giữ nguyên.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            await _householdRepository.UpdateMembersAsync(activeMembers);

            // 3. Chuyển trạng thái Household sang Inactive
            household.Status = "Inactive";
            await _householdRepository.UpdateHouseholdAsync(household);

            await _householdRepository.AddNotificationAsync(new Notification
            {
                UserId = creatorUserId,
                Message = $"Bạn đã xóa hộ gia đình \"{householdName}\".",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            return (true, $"Đã xóa hộ gia đình \"{householdName}\" thành công!");
        }

        // =========================================
        // UC-29: SET TRANSACTION ALERT THRESHOLD (BR-37)
        // =========================================
        public async Task<(bool Success, string Message)> SetMemberAlertThresholdAsync(
            int managerUserId,
            int targetHouseholdMemberId,
            decimal? alertAmount)
        {
            if (alertAmount.HasValue && alertAmount.Value < 0)
            {
                return (false, "Hạn mức cảnh báo giao dịch không được nhỏ hơn 0.");
            }

            var manager = await _householdRepository.GetActiveMemberByUserIdAsync(managerUserId);
            if (manager == null || !string.Equals(manager.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ Quản lý (Manager) mới có quyền thiết lập ngưỡng cảnh báo giao dịch.");
            }

            var targetMember = await _householdRepository.GetMemberByIdAsync(targetHouseholdMemberId);
            if (targetMember == null ||
                targetMember.HouseholdId != manager.HouseholdId ||
                targetMember.Status != "Active")
            {
                return (false, "Không tìm thấy thành viên hợp lệ trong hộ gia đình.");
            }

            targetMember.AlertAmount = alertAmount;
            await _householdRepository.UpdateMemberAsync(targetMember);

            // Kiểm tra ngay nếu thành viên có giao dịch vượt ngưỡng vừa đặt để thông báo cho Manager (BR-37)
            if (alertAmount.HasValue && alertAmount.Value > 0)
            {
                var expenses = await _householdRepository.GetExpensesByMemberIdAsync(targetHouseholdMemberId);
                var incomes = await _householdRepository.GetIncomesByMemberIdAsync(targetHouseholdMemberId);

                int highValueCount = expenses.Count(e => e.Amount >= alertAmount.Value)
                                   + incomes.Count(i => i.Amount >= alertAmount.Value);

                if (highValueCount > 0)
                {
                    await _householdRepository.AddNotificationAsync(new Notification
                    {
                        UserId = managerUserId,
                        Message = $"[Cảnh báo ngưỡng giao dịch] Thành viên {targetMember.User.FullName} hiện có {highValueCount} giao dịch lớn hơn hoặc bằng ngưỡng {alertAmount.Value:N0} VNĐ.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            string formattedThreshold = alertAmount.HasValue
                ? $"{alertAmount.Value:N0} VNĐ"
                : "Không giới hạn (Đã tắt cảnh báo)";

            return (
                true,
                $"Đã cập nhật ngưỡng cảnh báo giao dịch cho {targetMember.User.FullName}: {formattedThreshold}."
            );
        }

        // =========================================
        // UC-12 & UC-16: VIEW MEMBER WALLETS & TRANSACTIONS (READ-ONLY) (BR-16, BR-20)
        // =========================================
        public async Task<(bool Success, string Message, MemberFinanceDetailViewModel? Data)> GetMemberFinanceDetailAsync(
            int managerUserId,
            int targetHouseholdMemberId,
            int? walletId = null,
            string? transactionType = null,
            string? search = null)
        {
            var manager = await _householdRepository.GetActiveMemberByUserIdAsync(managerUserId);
            if (manager == null || !string.Equals(manager.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ Quản lý (Manager) mới có quyền xem thông tin ví và giao dịch của các thành viên trong hộ.", null);
            }

            // BR-11 & BR-16: Chỉ được xem thông tin của thành viên hiện đang Active trong cùng hộ gia đình
            var targetMember = await _householdRepository.GetMemberByIdAsync(targetHouseholdMemberId);
            if (targetMember == null ||
                targetMember.HouseholdId != manager.HouseholdId ||
                targetMember.Status != "Active")
            {
                return (false, "Thành viên này không tồn tại hoặc không còn thuộc hộ gia đình của bạn (BR-11).", null);
            }

            var wallets = await _householdRepository.GetWalletsByMemberIdAsync(targetMember.HouseholdMemberId);
            var incomes = await _householdRepository.GetIncomesByMemberIdAsync(targetMember.HouseholdMemberId);
            var expenses = await _householdRepository.GetExpensesByMemberIdAsync(targetMember.HouseholdMemberId);

            // Xác định tháng dùng để tính hạn mức chi tiêu tháng của ví (ưu tiên tháng hiện tại hoặc tháng có giao dịch gần nhất)
            DateOnly nowDate = DateOnly.FromDateTime(DateTime.Now);
            int targetYear = nowDate.Year;
            int targetMonth = nowDate.Month;

            var walletItems = wallets.Select(w =>
            {
                var walletIncomes = incomes.Where(i => i.WalletId == w.WalletId).ToList();
                var walletExpenses = expenses.Where(e => e.WalletId == w.WalletId).ToList();
                decimal monthlyExp = walletExpenses
                    .Where(e => e.ExpenseDate.Year == targetYear && e.ExpenseDate.Month == targetMonth)
                    .Sum(e => e.Amount);

                // Nếu tháng hiện tại chưa có giao dịch nhưng tổng chi > 0, hiển thị tổng chi trên ví để dễ theo dõi hạn mức
                if (monthlyExp == 0 && walletExpenses.Any())
                {
                    var latestMonth = walletExpenses.Max(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month });
                    if (latestMonth != null)
                    {
                        monthlyExp = walletExpenses
                            .Where(e => e.ExpenseDate.Year == latestMonth.Year && e.ExpenseDate.Month == latestMonth.Month)
                            .Sum(e => e.Amount);
                    }
                }

                return new MemberWalletItemViewModel
                {
                    WalletId = w.WalletId,
                    WalletName = w.WalletName,
                    CurrentBalance = w.Amount,
                    LimitAmount = w.LimitAmount,
                    TotalIncome = walletIncomes.Sum(i => i.Amount),
                    TotalExpense = walletExpenses.Sum(e => e.Amount),
                    MonthlyExpense = monthlyExp,
                    TransactionCount = walletIncomes.Count + walletExpenses.Count
                };
            }).ToList();

            decimal? alertThreshold = targetMember.AlertAmount;

            // Gộp danh sách Thu (Income) và Chi (Expense) thành lịch sử giao dịch chung
            var allTransactions = new List<MemberTransactionItemViewModel>();

            foreach (var inc in incomes)
            {
                allTransactions.Add(new MemberTransactionItemViewModel
                {
                    TransactionId = inc.IncomeId,
                    TransactionType = "Income",
                    TransactionDate = inc.IncomeDate,
                    Amount = inc.Amount,
                    Description = inc.Description,
                    WalletId = inc.WalletId,
                    WalletName = inc.Wallet?.WalletName ?? "Ví cá nhân",
                    CategoryName = "Khoản thu (Không phân loại)",
                    IsOverAlertThreshold = alertThreshold.HasValue && alertThreshold.Value > 0 && inc.Amount >= alertThreshold.Value
                });
            }

            foreach (var exp in expenses)
            {
                // BR-27: Nếu danh mục đã bị xóa/tắt (Inactive), hiển thị nhãn "Category Deleted"
                string catDisplay = exp.Category == null
                    ? "Category Deleted"
                    : (string.Equals(exp.Category.Status, "Inactive", StringComparison.OrdinalIgnoreCase)
                        ? $"{exp.Category.CategoryName} (Category Deleted)"
                        : exp.Category.CategoryName);

                allTransactions.Add(new MemberTransactionItemViewModel
                {
                    TransactionId = exp.ExpenseId,
                    TransactionType = "Expense",
                    TransactionDate = exp.ExpenseDate,
                    Amount = exp.Amount,
                    Description = exp.Description,
                    WalletId = exp.WalletId,
                    WalletName = exp.Wallet?.WalletName ?? "Ví cá nhân",
                    CategoryName = catDisplay,
                    IsOverAlertThreshold = alertThreshold.HasValue && alertThreshold.Value > 0 && exp.Amount >= alertThreshold.Value
                });
            }

            // Áp dụng bộ lọc theo Ví, Loại giao dịch, Từ khóa tìm kiếm
            IEnumerable<MemberTransactionItemViewModel> filtered = allTransactions;

            if (walletId.HasValue && walletId.Value > 0)
            {
                filtered = filtered.Where(t => t.WalletId == walletId.Value);
            }

            if (!string.IsNullOrWhiteSpace(transactionType) &&
                !string.Equals(transactionType, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(t => string.Equals(t.TransactionType, transactionType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(t =>
                    (t.Description != null && t.Description.ToLowerInvariant().Contains(kw)) ||
                    t.CategoryName.ToLowerInvariant().Contains(kw) ||
                    t.WalletName.ToLowerInvariant().Contains(kw));
            }

            var sortedTransactions = filtered
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.TransactionId)
                .ToList();

            var detailVm = new MemberFinanceDetailViewModel
            {
                HouseholdId = targetMember.HouseholdId,
                HouseholdName = manager.Household.HouseholdName,
                MemberId = targetMember.HouseholdMemberId,
                UserId = targetMember.UserId,
                FullName = targetMember.User.FullName,
                Email = targetMember.User.Email,
                Phone = targetMember.User.Phone,
                Role = targetMember.Role,
                JoinedAt = targetMember.JoinedAt,
                AlertAmount = targetMember.AlertAmount,
                TotalWalletBalance = wallets.Sum(w => w.Amount),
                TotalIncome = incomes.Sum(i => i.Amount),
                TotalExpense = expenses.Sum(e => e.Amount),
                SelectedWalletId = walletId,
                SelectedType = transactionType,
                SearchKeyword = search,
                Wallets = walletItems,
                Transactions = sortedTransactions
            };

            return (true, string.Empty, detailVm);
        }
    }
}

