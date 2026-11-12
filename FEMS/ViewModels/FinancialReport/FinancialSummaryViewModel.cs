namespace FEMS.ViewModels.FinancialReport
{
    public class FinancialSummaryViewModel
    {
        public int Months { get; set; } = 1;
        public string SelectedMonth { get; set; } = "";

        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public decimal NetIncome => TotalIncome - TotalExpense;

        public List<MonthlyFinancialViewModel> MonthlyData { get; set; }
            = new List<MonthlyFinancialViewModel>();

        public List<MemberFinancialViewModel> MemberData { get; set; }
            = new List<MemberFinancialViewModel>();

        // Phân quyền & Thông tin bổ sung cho Admin Dashboard
        public bool IsManager { get; set; }

        public string CurrentUserRole { get; set; } = "Member";

        public string HouseholdName { get; set; } = string.Empty;

        public int ActiveMemberCount { get; set; }

        public int PersonalWalletCount { get; set; }

        public List<DashboardWalletItemViewModel> Wallets { get; set; }
            = new List<DashboardWalletItemViewModel>();

        public List<DashboardTransactionItemViewModel> RecentTransactions { get; set; }
            = new List<DashboardTransactionItemViewModel>();

        public List<DashboardAlertItemViewModel> Alerts { get; set; }
            = new List<DashboardAlertItemViewModel>();

        public int UnreadAlertCount { get; set; }

        public List<CategoryExpenseViewModel> CategoryDistribution { get; set; }
            = new List<CategoryExpenseViewModel>();
    }

    public class MonthlyFinancialViewModel
    {
        public string Month { get; set; } = "";

        public decimal Income { get; set; }

        public decimal Expense { get; set; }
    }

    public class MemberFinancialViewModel
    {
        public string MemberName { get; set; } = "";

        public decimal Income { get; set; }

        public decimal Expense { get; set; }
    }

    public class DashboardWalletItemViewModel
    {
        public int WalletId { get; set; }

        public string MemberName { get; set; } = string.Empty;

        public string WalletName { get; set; } = string.Empty;

        public decimal CurrentBalance { get; set; }

        public decimal? LimitAmount { get; set; }

        public decimal MonthlyExpense { get; set; }

        public decimal? LimitUsagePercent =>
            LimitAmount.HasValue && LimitAmount.Value > 0
                ? Math.Round((MonthlyExpense / LimitAmount.Value) * 100m, 1)
                : null;

        public bool IsOverLimit =>
            LimitAmount.HasValue && LimitAmount.Value > 0 && MonthlyExpense > LimitAmount.Value;

        public bool IsNegativeBalance => CurrentBalance < 0;

        public bool IsAlert => IsNegativeBalance || IsOverLimit;
    }

    public class DashboardTransactionItemViewModel
    {
        public int TransactionId { get; set; }

        public DateOnly Date { get; set; }

        public string MemberName { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty; // "Income" | "Expense"

        public string CategoryName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string WalletName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsOverAlertThreshold { get; set; }
    }

    public class DashboardAlertItemViewModel
    {
        public int NotificationId { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public string AlertType { get; set; } = "Info"; // "Warning" | "Danger" | "Info"
    }
}
