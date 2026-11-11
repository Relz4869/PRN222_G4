
namespace FEMS.ViewModels.FinancialReport
{
    public class DetailedReportViewModel
    {
        public int Months { get; set; } = 3;

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
}
