using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Transaction;

namespace FEMS.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        // =========================================
        // 1. LẤY DỮ LIỆU MÀN HÌNH QUẢN LÝ GIAO DỊCH (VIEW OWN TRANSACTIONS)
        // =========================================
        public async Task<TransactionManagementViewModel> GetTransactionManagementDataAsync(
            int userId,
            int? filterWalletId = null,
            string? filterMonth = null)
        {
            var member = await _transactionRepository.GetMemberByUserIdAsync(userId);
            if (member == null)
            {
                return new TransactionManagementViewModel
                {
                    HasHouseholdMembership = false
                };
            }

            var wallets = await _transactionRepository.GetWalletsByMemberIdAsync(member.HouseholdMemberId);
            var activeCategories = await _transactionRepository.GetActiveCategoriesByMemberIdAsync(member.HouseholdMemberId);
            var incomes = await _transactionRepository.GetIncomesByMemberIdAsync(member.HouseholdMemberId);
            var expenses = await _transactionRepository.GetExpensesByMemberIdAsync(member.HouseholdMemberId);

            // Gộp danh sách Thu nhập (Income) và Chi tiêu (Expense) của người dùng
            var allTransactions = new List<OwnTransactionItemViewModel>();

            foreach (var inc in incomes)
            {
                allTransactions.Add(new OwnTransactionItemViewModel
                {
                    TransactionId = inc.IncomeId,
                    TransactionType = "Income",
                    TransactionDate = inc.IncomeDate,
                    WalletId = inc.WalletId,
                    WalletName = inc.Wallet?.WalletName ?? "Ví cá nhân",
                    CategoryDisplay = "— (Không áp dụng)",
                    IsCategoryDeleted = false,
                    Description = inc.Description,
                    Amount = inc.Amount
                });
            }

            foreach (var exp in expenses)
            {
                bool isDeletedCategory = exp.Category == null ||
                                         string.Equals(exp.Category.Status, "Inactive", StringComparison.OrdinalIgnoreCase);

                string categoryLabel = exp.Category == null
                    ? "[Category Deleted]"
                    : (isDeletedCategory ? "[Category Deleted]" : exp.Category.CategoryName);

                allTransactions.Add(new OwnTransactionItemViewModel
                {
                    TransactionId = exp.ExpenseId,
                    TransactionType = "Expense",
                    TransactionDate = exp.ExpenseDate,
                    WalletId = exp.WalletId,
                    WalletName = exp.Wallet?.WalletName ?? "Ví cá nhân",
                    CategoryDisplay = categoryLabel,
                    IsCategoryDeleted = isDeletedCategory,
                    Description = exp.Description,
                    Amount = exp.Amount
                });
            }

            // Tạo danh sách các Tháng/Năm để hiển thị trên bộ lọc
            var monthSet = allTransactions
                .Select(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Distinct()
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();

            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            if (!monthSet.Any(m => m.Year == today.Year && m.Month == today.Month))
            {
                monthSet.Insert(0, new { today.Year, today.Month });
            }

            var availableMonths = monthSet
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .Select(m => new MonthFilterOption
                {
                    Value = $"{m.Year:D4}-{m.Month:D2}",
                    Label = $"Tháng {m.Month:D2}/{m.Year:D4}"
                })
                .ToList();

            // Áp dụng bộ lọc theo Ví cá nhân và Tháng/Năm
            IEnumerable<OwnTransactionItemViewModel> filtered = allTransactions;

            if (filterWalletId.HasValue && filterWalletId.Value > 0)
            {
                filtered = filtered.Where(t => t.WalletId == filterWalletId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filterMonth) &&
                !string.Equals(filterMonth, "All", StringComparison.OrdinalIgnoreCase))
            {
                var parts = filterMonth.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int fYear) &&
                    int.TryParse(parts[1], out int fMonth))
                {
                    filtered = filtered.Where(t =>
                        t.TransactionDate.Year == fYear &&
                        t.TransactionDate.Month == fMonth);
                }
            }

            var sortedTransactions = filtered
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.TransactionId)
                .ToList();

            return new TransactionManagementViewModel
            {
                HouseholdMemberId = member.HouseholdMemberId,
                HasHouseholdMembership = true,
                TransactionType = "Expense",
                WalletId = wallets.FirstOrDefault()?.WalletId ?? 0,
                CategoryId = activeCategories.FirstOrDefault()?.CategoryId,
                TransactionDate = today,
                Wallets = wallets,
                ActiveCategories = activeCategories,
                FilterWalletId = filterWalletId,
                FilterMonth = filterMonth,
                AvailableMonths = availableMonths,
                Transactions = sortedTransactions
            };
        }

        // =========================================
        // 2. GHI NHẬN GIAO DỊCH MỚI (CREATE TRANSACTION)
        // Cập nhật số dư ví, kiểm tra hạn mức ví, trạng thái số dư âm và ngưỡng cảnh báo giao dịch
        // =========================================
        public async Task<(bool Success, string Message)> CreateTransactionAsync(
            int userId,
            TransactionManagementViewModel input)
        {
            var member = await _transactionRepository.GetMemberByUserIdAsync(userId);
            if (member == null)
            {
                return (false, "Bạn cần tham gia hoặc tạo một Hộ gia đình trước khi ghi nhận giao dịch.");
            }

            if (input.Amount <= 0)
            {
                return (false, "Số tiền giao dịch phải lớn hơn 0 VNĐ.");
            }

            var wallet = await _transactionRepository.GetWalletByIdAsync(input.WalletId);
            if (wallet == null || wallet.HouseholdMemberId != member.HouseholdMemberId)
            {
                return (false, "Vui lòng chọn một ví cá nhân hợp lệ của bạn.");
            }

            bool isExpense = !string.Equals(input.TransactionType, "Income", StringComparison.OrdinalIgnoreCase);
            string? cleanDescription = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            string categoryNameForAlert = "Khoản thu";

            if (isExpense)
            {
                // Giao dịch Chi tiêu (Expense) bắt buộc phải có Danh mục (Category)
                if (!input.CategoryId.HasValue || input.CategoryId.Value <= 0)
                {
                    return (false, "Giao dịch Chi tiêu (Expense) bắt buộc phải chọn một Danh mục chi tiêu.");
                }

                var category = await _transactionRepository.GetCategoryByIdAsync(input.CategoryId.Value);
                if (category == null ||
                    category.HouseholdMemberId != member.HouseholdMemberId ||
                    !string.Equals(category.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Danh mục chi tiêu đã chọn không hợp lệ hoặc đã ngừng hoạt động.");
                }

                categoryNameForAlert = category.CategoryName;

                var newExpense = new Expense
                {
                    HouseholdMemberId = member.HouseholdMemberId,
                    CategoryId = category.CategoryId,
                    WalletId = wallet.WalletId,
                    Amount = input.Amount,
                    ExpenseDate = input.TransactionDate,
                    Description = cleanDescription
                };

                await _transactionRepository.AddExpenseAsync(newExpense);

                // Cập nhật trừ số dư ví
                wallet.Amount -= input.Amount;
                await _transactionRepository.UpdateWalletAsync(wallet);
            }
            else
            {
                // Giao dịch Thu nhập (Income) không sử dụng Danh mục
                var newIncome = new Income
                {
                    HouseholdMemberId = member.HouseholdMemberId,
                    WalletId = wallet.WalletId,
                    Amount = input.Amount,
                    IncomeDate = input.TransactionDate,
                    Description = cleanDescription
                };

                await _transactionRepository.AddIncomeAsync(newIncome);

                // Cập nhật cộng số dư ví
                wallet.Amount += input.Amount;
                await _transactionRepository.UpdateWalletAsync(wallet);
            }

            // =========================================
            // KIỂM TRA HẠN MỨC CHI TIÊU VÍ, SỐ DƯ ÂM & NGƯỠNG GIAO DỊCH GIÁ TRỊ LỚN (TRIGGERS ALERTS)
            // =========================================
            var allMemberExpenses = await _transactionRepository.GetExpensesByMemberIdAsync(member.HouseholdMemberId);
            decimal monthlyWalletExpense = allMemberExpenses
                .Where(e => e.WalletId == wallet.WalletId &&
                            e.ExpenseDate.Year == input.TransactionDate.Year &&
                            e.ExpenseDate.Month == input.TransactionDate.Month)
                .Sum(e => e.Amount);

            bool isNegativeBalance = wallet.Amount < 0;
            bool isOverSpendingLimit = isExpense &&
                                       wallet.LimitAmount.HasValue &&
                                       wallet.LimitAmount.Value > 0 &&
                                       monthlyWalletExpense > wallet.LimitAmount.Value;

            bool isHighValueTransaction = member.AlertAmount.HasValue &&
                                          member.AlertAmount.Value > 0 &&
                                          input.Amount >= member.AlertAmount.Value;

            // 1. Tạo thông báo cho chính Chủ ví (User đang đăng nhập) nếu vượt hạn mức chi tiêu hoặc âm số dư
            if (isNegativeBalance && isOverSpendingLimit)
            {
                await _transactionRepository.AddNotificationAsync(new Notification
                {
                    UserId = userId,
                    Message = $"[Cảnh báo Kép - Ví {wallet.WalletName}] Sau khoản chi {input.Amount:N0} VNĐ, ví của bạn vừa bị âm số dư ({wallet.Amount:N0} VNĐ) vừa vượt hạn mức chi tiêu tháng ({monthlyWalletExpense:N0}/{wallet.LimitAmount!.Value:N0} VNĐ)!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else if (isNegativeBalance)
            {
                await _transactionRepository.AddNotificationAsync(new Notification
                {
                    UserId = userId,
                    Message = $"[Cảnh báo Số dư âm - BR-18] Ví \"{wallet.WalletName}\" hiện có số dư âm ({wallet.Amount:N0} VNĐ) sau giao dịch {input.Amount:N0} VNĐ.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else if (isOverSpendingLimit)
            {
                await _transactionRepository.AddNotificationAsync(new Notification
                {
                    UserId = userId,
                    Message = $"[Cảnh báo Vượt hạn mức ví] Tổng chi tiêu tháng {input.TransactionDate:MM/yyyy} trên ví \"{wallet.WalletName}\" ({monthlyWalletExpense:N0} VNĐ) đã vượt hạn mức ({wallet.LimitAmount!.Value:N0} VNĐ).",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            // 2. Tạo thông báo cho Quản lý hộ (Manager) nếu giao dịch đạt hoặc vượt ngưỡng cảnh báo (High-Value / Combined Alert)
            if (isHighValueTransaction && string.Equals(member.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                var managers = await _transactionRepository.GetActiveManagersByHouseholdIdAsync(member.HouseholdId);
                string memberName = member.User?.FullName ?? "Thành viên";
                string txTypeLabel = isExpense ? $"khoản chi ({categoryNameForAlert})" : "khoản thu";

                foreach (var mgr in managers)
                {
                    string mgrAlertMessage;
                    if (isOverSpendingLimit || isNegativeBalance)
                    {
                        mgrAlertMessage =
                            $"[Cảnh báo Kép - Giao dịch lớn & Ngân sách] Thành viên {memberName} vừa ghi nhận {txTypeLabel} {input.Amount:N0} VNĐ trên ví \"{wallet.WalletName}\" (vượt ngưỡng cảnh báo {member.AlertAmount!.Value:N0} VNĐ và vượt ngưỡng an toàn ví).";
                    }
                    else
                    {
                        mgrAlertMessage =
                            $"[Cảnh báo Giao dịch lớn] Thành viên {memberName} vừa ghi nhận {txTypeLabel} trị giá {input.Amount:N0} VNĐ trên ví \"{wallet.WalletName}\" (Ngưỡng cảnh báo: {member.AlertAmount!.Value:N0} VNĐ).";
                    }

                    await _transactionRepository.AddNotificationAsync(new Notification
                    {
                        UserId = mgr.UserId,
                        Message = mgrAlertMessage,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            string actionLabel = isExpense ? "Chi tiêu" : "Thu nhập";
            string signPrefix = isExpense ? "-" : "+";

            return (
                true,
                $"Đã ghi nhận giao dịch {actionLabel} ({signPrefix}{input.Amount:N0} VNĐ) vào \"{wallet.WalletName}\". Số dư mới: {wallet.Amount:N0} VNĐ."
            );
        }
    }
}

