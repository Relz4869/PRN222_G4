namespace FEMS.ViewModels.Household
{
    public class MemberWalletItemViewModel
    {
        public int WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal? LimitAmount { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal MonthlyExpense { get; set; }
        public int TransactionCount { get; set; }

        public decimal? LimitUsagePercent =>
            LimitAmount.HasValue && LimitAmount.Value > 0
                ? Math.Round((MonthlyExpense / LimitAmount.Value) * 100m, 1)
                : null;

        public bool IsOverLimit =>
            LimitAmount.HasValue && LimitAmount.Value > 0 && MonthlyExpense > LimitAmount.Value;
    }

    public class MemberTransactionItemViewModel
    {
        public int TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Income" | "Expense"
        public DateOnly TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int? WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsOverAlertThreshold { get; set; }
    }

    public class MemberFinanceDetailViewModel
    {
        public int HouseholdId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;

        public int MemberId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public decimal? AlertAmount { get; set; }

        public decimal TotalWalletBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }

        // Bộ lọc cho lịch sử giao dịch (Read-only)
        public int? SelectedWalletId { get; set; }
        public string? SelectedType { get; set; } // null / "All", "Income", "Expense"
        public string? SearchKeyword { get; set; }

        public List<MemberWalletItemViewModel> Wallets { get; set; } = new();
        public List<MemberTransactionItemViewModel> Transactions { get; set; } = new();
    }
}

