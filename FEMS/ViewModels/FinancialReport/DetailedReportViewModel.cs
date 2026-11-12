
namespace FEMS.ViewModels.FinancialReport
{
    public class DetailedReportViewModel
    {
        public int Months { get; set; } = 1;
        public string SelectedMonth { get; set; } = "";
        public List<MemberOptionViewModel> Members { get; set; } = new();

        public List<ExpenseDetailViewModel> Expenses { get; set; } = new();
        public int? SelectedMemberId { get; set; }
        public bool IsManager { get; set; }

        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public List<IncomeDetailViewModel> Incomes { get; set; }
            = new List<IncomeDetailViewModel>();

        public List<CategoryExpenseViewModel> Categories { get; set; }
            = new List<CategoryExpenseViewModel>();
    }

    public class IncomeDetailViewModel
    {
        public DateOnly Date { get; set; }

        public string MemberName { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Amount { get; set; }
    }

    public class CategoryExpenseViewModel
    {
        public string CategoryName { get; set; } = "";

        public decimal TotalAmount { get; set; }

        public int TransactionCount { get; set; }
    }

    public class MemberOptionViewModel
    {
        public int HouseholdMemberId { get; set; }

        public string MemberName { get; set; } = "";

        public string Status { get; set; } = "";
    }

    public class ExpenseDetailViewModel
    {
        public DateOnly Date { get; set; }

        public string MemberName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Amount { get; set; }
    }

}
