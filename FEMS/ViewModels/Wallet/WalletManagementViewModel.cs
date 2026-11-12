using System.ComponentModel.DataAnnotations;

namespace FEMS.ViewModels.Wallet
{
    public class WalletCardItemViewModel
    {
        public int WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal? LimitAmount { get; set; }
        public decimal MonthlyExpense { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public int TransactionCount { get; set; }

        // BR-18: Highlight ví màu đỏ khi Số dư âm hoặc Vượt hạn mức chi tiêu tháng
        public bool IsNegativeBalance => CurrentBalance < 0;

        public bool IsOverLimit =>
            LimitAmount.HasValue && LimitAmount.Value > 0 && MonthlyExpense > LimitAmount.Value;

        public bool IsAlertCard => IsNegativeBalance || IsOverLimit;

        public decimal? LimitUsagePercent =>
            LimitAmount.HasValue && LimitAmount.Value > 0
                ? Math.Round((MonthlyExpense / LimitAmount.Value) * 100m, 1)
                : null;
    }

    public class WalletHistoryItemViewModel
    {
        public int RecordId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string DisplayTime { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty; // "Income" | "Expense" | "Adjustment" | "Transfer"
        public string ChangeTypeLabel { get; set; } = string.Empty;
        public string CategoryOrTitle { get; set; } = string.Empty;
        public string DetailDescription { get; set; } = string.Empty;
        public decimal SignedAmount { get; set; } // Dương (+) hoặc Âm (-)
        public decimal BalanceAfter { get; set; } // Số dư sau GD
    }

    public class WalletManagementViewModel
    {
        public int HouseholdMemberId { get; set; }
        public bool HasHouseholdMembership { get; set; }

        // Danh sách thẻ ví cá nhân ở trên
        public List<WalletCardItemViewModel> Wallets { get; set; } = new();

        // Ví đang được chọn để xem Lịch sử biến động & Thiết lập hạn mức
        public int? SelectedWalletId { get; set; }
        public WalletCardItemViewModel? SelectedWallet { get; set; }

        // Lịch sử biến động của Ví đang chọn
        public List<WalletHistoryItemViewModel> WalletHistories { get; set; } = new();

        // =========================================
        // 1. FORM TẠO VÍ CÁ NHÂN (Create Personal Wallet)
        // =========================================
        [Required(ErrorMessage = "Vui lòng nhập tên ví cá nhân.")]
        [StringLength(100, ErrorMessage = "Tên ví không được vượt quá 100 ký tự.")]
        public string NewWalletName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số dư ban đầu.")]
        [Range(1, double.MaxValue, ErrorMessage = "Số dư ban đầu phải lớn hơn 0.")]
        public decimal NewInitialBalance { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng nhập hạn mức chi tiêu.")]
        [Range(1, double.MaxValue, ErrorMessage = "Hạn mức chi tiêu phải lớn hơn 0.")]
        public decimal? NewLimitAmount { get; set; }

        // =========================================
        // 2. FORM CẬP NHẬT HẠN MỨC CHI TIÊU VÍ (Wallet Spending Limit)
        // =========================================
        public int EditWalletId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên ví cá nhân.")]
        [StringLength(100, ErrorMessage = "Tên ví không được vượt quá 100 ký tự.")]
        public string EditWalletName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập hạn mức chi tiêu tháng.")]
        [Range(1, double.MaxValue, ErrorMessage = "Hạn mức chi tiêu phải lớn hơn 0.")]
        public decimal? EditLimitAmount { get; set; }
    }
}

