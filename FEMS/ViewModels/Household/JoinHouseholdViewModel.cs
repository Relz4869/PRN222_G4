using System.ComponentModel.DataAnnotations;

namespace FEMS.ViewModels.Household
{
    public class PendingInviteItemViewModel
    {
        public int NotificationId { get; set; }
        public int HouseholdId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class JoinHouseholdViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã tham gia hộ gia đình.")]
        [StringLength(20, ErrorMessage = "Mã tham gia không hợp lệ.")]
        [Display(Name = "Mã tham gia")]
        public string InviteCode { get; set; } = string.Empty;

        public List<PendingInviteItemViewModel> PendingInvites { get; set; } = new();
    }
}

