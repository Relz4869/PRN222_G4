
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
    int userId, int months, string? selectedMonth)
        {
            if (months != 1 && months != 3 &&
                months != 6 && months != 12)
            {
                months = 1;
            }

            var currentMember = await _householdRepository
                .GetActiveMemberByUserIdAsync(userId);

            if (currentMember == null)
            {
                return null;
            }

            var members = await _householdRepository
     .GetMembersByHouseholdIdAsync(
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

            var selectedDate = new DateOnly(today.Year, today.Month, 1);

            if (!string.IsNullOrWhiteSpace(selectedMonth) &&
                DateOnly.TryParseExact(
                    selectedMonth,
                    "yyyy-MM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedMonth))
            {
                selectedDate = new DateOnly(parsedMonth.Year, parsedMonth.Month, 1);
            }

            var startDate = selectedDate.AddMonths(-(months - 1));
            var endDate = selectedDate.AddMonths(1);


            var incomes = await _reportRepository
                .GetIncomesAsync(memberIds, startDate, endDate);

            var expenses = await _reportRepository
                .GetExpensesAsync(memberIds, startDate, endDate);

           
            var model = new FinancialSummaryViewModel
            {
                Months = months,
                SelectedMonth = selectedDate.ToString("yyyy-MM"),
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
            int userId, int months, string? selectedMonth, int? selectedMemberId)
        {
            if (months != 1 && months != 3 &&
                months != 6 && months != 12)
            {
                months = 1;
            }

            var currentMember = await _householdRepository
                .GetActiveMemberByUserIdAsync(userId);

            if (currentMember == null)
            {
                return null;
            }

            bool isManager = currentMember.Role == "Manager";

            var allMembers = await _householdRepository
                .GetMembersByHouseholdIdAsync(currentMember.HouseholdId);

            var members = isManager
                ? allMembers
                : allMembers.Where(m => m.UserId == userId).ToList();

            if (isManager && selectedMemberId.HasValue)
            {
                members = members
                    .Where(m => m.HouseholdMemberId == selectedMemberId.Value)
                    .ToList();
            }

            var memberIds = members
                .Select(m => m.HouseholdMemberId)
                .ToList();

            var today = DateTime.Today;
            var selectedDate = new DateOnly(today.Year, today.Month, 1);

            if (!string.IsNullOrWhiteSpace(selectedMonth) &&
                DateOnly.TryParseExact(
                    selectedMonth,
                    "yyyy-MM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedMonth))
            {
                selectedDate = new DateOnly(
                    parsedMonth.Year, parsedMonth.Month, 1);
            }

            var startDate = selectedDate.AddMonths(-(months - 1));
            var endDate = selectedDate.AddMonths(1);

            var incomes = await _reportRepository
                .GetIncomesAsync(memberIds, startDate, endDate);

            var expenses = await _reportRepository
                .GetExpensesAsync(memberIds, startDate, endDate);

            var model = new DetailedReportViewModel
            {
                Months = months,
                SelectedMonth = selectedDate.ToString("yyyy-MM"),
                SelectedMemberId = isManager ? selectedMemberId : null,
                IsManager = isManager,
                TotalIncome = incomes.Sum(i => i.Amount),
                TotalExpense = expenses.Sum(e => e.Amount)
            };

            if (isManager)
            {
                model.Members = allMembers
                    .Select(m => new MemberOptionViewModel
                    {
                        HouseholdMemberId = m.HouseholdMemberId,
                        MemberName = m.User.FullName,
                        Status = m.Status
                    })
                    .ToList();
            }

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

            foreach (var expense in expenses.OrderByDescending(e => e.ExpenseDate))
            {
                var member = members.First(m =>
                    m.HouseholdMemberId == expense.HouseholdMemberId);

                model.Expenses.Add(new ExpenseDetailViewModel
                {
                    Date = expense.ExpenseDate,
                    MemberName = member.User.FullName,
                    CategoryName = expense.Category.CategoryName,
                    Description = expense.Description ?? "",
                    Amount = expense.Amount
                });
            }

            model.Categories = expenses
                .GroupBy(e => new
                {
                    e.CategoryId,
                    e.Category.CategoryName
                })
                .Select(g => new CategoryExpenseViewModel
                {
                    CategoryName = g.Key.CategoryName,
                    TotalAmount = g.Sum(e => e.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            return model;
        }


    }
}
