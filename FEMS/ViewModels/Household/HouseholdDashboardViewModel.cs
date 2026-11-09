using System.ComponentModel.DataAnnotations;
using FEMS.Models.Entities;

namespace FEMS.ViewModels.Household
{
    public class HouseholdMemberItemViewModel
    {
        public int HouseholdMemberId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public decimal? AlertAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCreator { get; set; }
    }

    public class SentInviteItemViewModel
    {
        public int NotificationId { get; set; }
        public int InvitedUserId { get; set; }
        public string InvitedFullName { get; set; } = string.Empty;
        public string InvitedEmail { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class HouseholdDashboardViewModel
    {
        public int HouseholdId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int CurrentUserId { get; set; }
        public int CurrentMemberId { get; set; }
        public string CurrentUserRole { get; set; } = string.Empty;
        public int CreatorMemberId { get; set; }
        public int CreatorUserId { get; set; }
        public bool IsManager => string.Equals(CurrentUserRole, "Manager", StringComparison.OrdinalIgnoreCase);
        public bool IsCreator => CurrentUserId == CreatorUserId;

        public List<HouseholdMemberItemViewModel> Members { get; set; } = new();

        public List<SentInviteItemViewModel> SentInvites { get; set; } = new();

        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
        public string? InviteEmail { get; set; }
    }
}

