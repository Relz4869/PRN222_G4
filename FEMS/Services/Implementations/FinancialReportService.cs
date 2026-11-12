
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
                IsManager = isManager,
                CurrentUserRole = currentMember.Role,
                HouseholdName = currentMember.Household?.HouseholdName ?? string.Empty,
                ActiveMemberCount = isManager ? members.Count : 1,
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

            // Phân bổ chi tiêu theo danh mục (Category Distribution)
            model.CategoryDistribution = expenses
                .GroupBy(e => e.Category != null && e.Category.Status == "Active" ? e.Category.CategoryName : "Khác")
                .Select(g => new CategoryExpenseViewModel
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(e => e.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            // Danh sách ví: Manager xem tất cả ví của hộ (Read-only); Member xem ví cá nhân
            DateOnly now = DateOnly.FromDateTime(today);
            if (isManager)
            {
                foreach (var member in members)
                {
                    var memberWallets = await _householdRepository.GetWalletsByMemberIdAsync(member.HouseholdMemberId);
                    foreach (var w in memberWallets)
                    {
                        decimal monthlySpent = expenses
                            .Where(e => e.WalletId == w.WalletId && e.ExpenseDate.Year == now.Year && e.ExpenseDate.Month == now.Month)
                            .Sum(e => e.Amount);

                        model.Wallets.Add(new DashboardWalletItemViewModel
                        {
                            WalletId = w.WalletId,
                            MemberName = member.User.FullName,
                            WalletName = w.WalletName,
                            CurrentBalance = w.Amount,
                            LimitAmount = w.LimitAmount,
                            MonthlyExpense = monthlySpent
                        });
                    }
                }
            }
            else
            {
                var personalWallets = await _householdRepository.GetWalletsByMemberIdAsync(currentMember.HouseholdMemberId);
                model.PersonalWalletCount = personalWallets.Count;
                foreach (var w in personalWallets)
                {
                    decimal monthlySpent = expenses
                        .Where(e => e.WalletId == w.WalletId && e.ExpenseDate.Year == now.Year && e.ExpenseDate.Month == now.Month)
                        .Sum(e => e.Amount);

                    model.Wallets.Add(new DashboardWalletItemViewModel
                    {
                        WalletId = w.WalletId,
                        MemberName = currentMember.User.FullName,
                        WalletName = w.WalletName,
                        CurrentBalance = w.Amount,
                        LimitAmount = w.LimitAmount,
                        MonthlyExpense = monthlySpent
                    });
                }
            }

            // Giao dịch gần nhất (Recent Transactions: top 7)
            var recentList = new List<DashboardTransactionItemViewModel>();
            foreach (var inc in incomes)
            {
                recentList.Add(new DashboardTransactionItemViewModel
                {
                    TransactionId = inc.IncomeId,
                    Date = inc.IncomeDate,
                    MemberName = inc.HouseholdMember?.User?.FullName ?? "",
                    Type = "Income",
                    CategoryName = "Thu nhập",
                    Amount = inc.Amount,
                    WalletName = inc.Wallet?.WalletName ?? "Ví cá nhân",
                    Description = inc.Description ?? "",
                    IsOverAlertThreshold = currentMember.AlertAmount.HasValue && inc.Amount >= currentMember.AlertAmount.Value
                });
            }
            foreach (var exp in expenses)
            {
                recentList.Add(new DashboardTransactionItemViewModel
                {
                    TransactionId = exp.ExpenseId,
                    Date = exp.ExpenseDate,
                    MemberName = exp.HouseholdMember?.User?.FullName ?? "",
                    Type = "Expense",
                    CategoryName = exp.Category?.CategoryName ?? "Chi tiêu",
                    Amount = exp.Amount,
                    WalletName = exp.Wallet?.WalletName ?? "Ví cá nhân",
                    Description = exp.Description ?? "",
                    IsOverAlertThreshold = currentMember.AlertAmount.HasValue && exp.Amount >= currentMember.AlertAmount.Value
                });
            }
            model.RecentTransactions = recentList
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.TransactionId)
                .Take(7)
                .ToList();

            // Thông báo & Cảnh báo (Alerts & Notifications)
            var notifications = await _householdRepository.GetNotificationsByUserIdAsync(userId);
            model.UnreadAlertCount = notifications.Count(n => !n.IsRead);
            model.Alerts = notifications
                .Take(5)
                .Select(n => new DashboardAlertItemViewModel
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,
                    AlertType = (n.Message.Contains("Cảnh báo", StringComparison.OrdinalIgnoreCase) ||
                                 n.Message.Contains("vượt", StringComparison.OrdinalIgnoreCase) ||
                                 n.Message.Contains("âm", StringComparison.OrdinalIgnoreCase))
                                ? "Warning" : "Info"
                })
                .ToList();

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
