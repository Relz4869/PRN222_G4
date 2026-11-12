
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
}
