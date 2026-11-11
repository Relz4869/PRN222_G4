
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.FinancialReport;

namespace FEMS.Services.Implementations
{
    public class FinancialReportService : IFinancialReportService
    {
        private readonly IFinancialReportRepository _reportRepository;
        private readonly IHouseholdRepository _householdRepository;

        public FinancialReportService(
            IFinancialReportRepository reportRepository,
            IHouseholdRepository householdRepository)
        {
            _reportRepository = reportRepository;
            _householdRepository = householdRepository;
        }

        public async Task<FinancialSummaryViewModel?> GetSummaryAsync(
            int userId, int months)
        {
            if (months != 3 && months != 6 && months != 12)
            {
                months = 3;
            }

            var currentMember = await _householdRepository
                .GetActiveMemberByUserIdAsync(userId);

            if (currentMember == null)
            {
                return null;
            }

            var members = await _householdRepository
                .GetActiveMembersByHouseholdIdAsync(
                    currentMember.HouseholdId);

            bool isManager = currentMember.Role == "Manager";

            if (!isManager)
            {
                members = members
                    .Where(m => m.UserId == userId)
                    .ToList();
            }

            var memberIds = members
                .Select(m => m.HouseholdMemberId)
                .ToList();

            DateTime today = DateTime.Today;

            var startDate = new DateOnly(
                today.Year, today.Month, 1)
                .AddMonths(-(months - 1));

            var endDate = new DateOnly(
                today.Year, today.Month, 1)
                .AddMonths(1);

            var incomes = await _reportRepository
                .GetIncomesAsync(memberIds, startDate, endDate);

            var expenses = await _reportRepository
                .GetExpensesAsync(memberIds, startDate, endDate);

            var model = new FinancialSummaryViewModel
            {
                Months = months,
                TotalIncome = incomes.Sum(i => i.Amount),
                TotalExpense = expenses.Sum(e => e.Amount)
            };

            for (int i = 0; i < months; i++)
            {
                var month = startDate.AddMonths(i);

                model.MonthlyData.Add(new MonthlyFinancialViewModel
                {
                    Month = month.ToString("MM/yyyy"),
                    Income = incomes
                        .Where(x => x.IncomeDate.Year == month.Year
                            && x.IncomeDate.Month == month.Month)
                        .Sum(x => x.Amount),
                    Expense = expenses
                        .Where(x => x.ExpenseDate.Year == month.Year
                            && x.ExpenseDate.Month == month.Month)
                        .Sum(x => x.Amount)
                });
            }

            if (isManager)
            {
                foreach (var member in members)
                {
                    model.MemberData.Add(new MemberFinancialViewModel
                    {
                        MemberName = member.User.FullName,
                        Income = incomes
                            .Where(x => x.HouseholdMemberId ==
                                member.HouseholdMemberId)
                            .Sum(x => x.Amount),
                        Expense = expenses
                            .Where(x => x.HouseholdMemberId ==
                                member.HouseholdMemberId)
                            .Sum(x => x.Amount)
                    });
                }
            }

            return model;
        }


        public async Task<DetailedReportViewModel?> GetDetailedReportAsync(
            int userId, int months)
        {
            if (months != 3 && months != 6 && months != 12)
            {
                months = 3;
            }

            var currentMember = await _householdRepository
                .GetActiveMemberByUserIdAsync(userId);

            if (currentMember == null)
            {
                return null;
            }

            bool isManager = string.Equals(
                currentMember.Role,
                "Manager",
                StringComparison.OrdinalIgnoreCase);

            var members = await _householdRepository
                .GetActiveMembersByHouseholdIdAsync(currentMember.HouseholdId);

            if (!isManager)
            {
                members = members
                    .Where(m => m.UserId == userId)
                    .ToList();
            }

            var memberIds = members
                .Select(m => m.HouseholdMemberId)
                .ToList();

            var today = DateTime.Today;

            var startDate = new DateOnly(
                today.Year, today.Month, 1)
                .AddMonths(-(months - 1));

            var endDate = new DateOnly(
                today.Year, today.Month, 1)
                .AddMonths(1);

            var incomes = await _reportRepository
                .GetIncomesAsync(memberIds, startDate, endDate);

            var expenses = await _reportRepository
                .GetExpensesAsync(memberIds, startDate, endDate);

            var model = new DetailedReportViewModel
            {
                Months = months,
                IsManager = isManager,
                TotalIncome = incomes.Sum(i => i.Amount),
                TotalExpense = expenses.Sum(e => e.Amount)
            };

            foreach (var income in incomes.OrderByDescending(i => i.IncomeDate))
            {
                var member = members.First(m =>
                    m.HouseholdMemberId == income.HouseholdMemberId);

                model.Incomes.Add(new IncomeDetailViewModel
                {
                    Date = income.IncomeDate,
                    MemberName = member.User.FullName,
                    Description = income.Description ?? "",
                    Amount = income.Amount
                });
            }

            model.Categories = expenses
                .GroupBy(e => new
                {
                    e.CategoryId,
                    e.Category.CategoryName,
                    e.Category.Status
                })
                .Select(g => new CategoryExpenseViewModel
                {
                    CategoryName = g.Key.Status == "Active"
                        ? g.Key.CategoryName
                        : "Category Deleted",
                    TotalAmount = g.Sum(e => e.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            return model;
        }

    }
}
