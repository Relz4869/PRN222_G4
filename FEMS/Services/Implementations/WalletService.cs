using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using FEMS.Services.Interfaces;
using FEMS.ViewModels.Wallet;

namespace FEMS.Services.Implementations
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;

        public WalletService(IWalletRepository walletRepository)
        {
            _walletRepository = walletRepository;
        }

        public async Task<HouseholdMember?> GetMemberByUserIdAsync(int userId)
        {
            return await _walletRepository.GetMemberByUserIdAsync(userId);
        }

        // =========================================
        // LẤY DỮ LIỆU QUẢN LÝ VÍ CÁ NHÂN & LỊCH SỬ BIẾN ĐỘNG
        // =========================================
        public async Task<WalletManagementViewModel> GetWalletManagementDataAsync(int userId, int? selectedWalletId = null)
        {
            var member = await _walletRepository.GetMemberByUserIdAsync(userId);
            if (member == null)
            {
                return new WalletManagementViewModel
                {
                    HasHouseholdMembership = false
                };
            }

            var wallets = await _walletRepository.GetWalletsByMemberIdAsync(member.HouseholdMemberId);
            var incomes = await _walletRepository.GetIncomesByMemberIdAsync(member.HouseholdMemberId);
            var expenses = await _walletRepository.GetExpensesByMemberIdAsync(member.HouseholdMemberId);

            DateOnly nowDate = DateOnly.FromDateTime(DateTime.Now);
            int targetYear = nowDate.Year;
            int targetMonth = nowDate.Month;

            var walletCards = wallets.Select(w =>
            {
                var walletIncomes = incomes.Where(i => i.WalletId == w.WalletId).ToList();
                var walletExpenses = expenses.Where(e => e.WalletId == w.WalletId).ToList();

                decimal monthlyExpense = walletExpenses
                    .Where(e => e.ExpenseDate.Year == targetYear && e.ExpenseDate.Month == targetMonth)
                    .Sum(e => e.Amount);

                // Nếu tháng hiện tại chưa có giao dịch nhưng có chi tiêu trước đó, lấy tháng gần nhất để theo dõi hạn mức
                if (monthlyExpense == 0 && walletExpenses.Any())
                {
                    var latestMonth = walletExpenses
                        .OrderByDescending(e => e.ExpenseDate)
                        .Select(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                        .FirstOrDefault();

                    if (latestMonth != null)
                    {
                        monthlyExpense = walletExpenses
                            .Where(e => e.ExpenseDate.Year == latestMonth.Year && e.ExpenseDate.Month == latestMonth.Month)
                            .Sum(e => e.Amount);
                    }
                }

                return new WalletCardItemViewModel
                {
                    WalletId = w.WalletId,
                    WalletName = w.WalletName,
                    CurrentBalance = w.Amount,
                    LimitAmount = w.LimitAmount,
                    MonthlyExpense = monthlyExpense,
                    TotalIncome = walletIncomes.Sum(i => i.Amount),
                    TotalExpense = walletExpenses.Sum(e => e.Amount),
                    TransactionCount = walletIncomes.Count + walletExpenses.Count
                };
            }).ToList();

            // Xác định ví đang được chọn (mặc định là ví đầu tiên nếu chưa truyền selectedWalletId)
            var activeWallet = selectedWalletId.HasValue
                ? walletCards.FirstOrDefault(w => w.WalletId == selectedWalletId.Value) ?? walletCards.FirstOrDefault()
                : walletCards.FirstOrDefault();

            var histories = new List<WalletHistoryItemViewModel>();

            if (activeWallet != null)
            {
                histories = BuildWalletHistory(
                    activeWallet,
                    incomes.Where(i => i.WalletId == activeWallet.WalletId).ToList(),
                    expenses.Where(e => e.WalletId == activeWallet.WalletId).ToList()
                );
            }

            return new WalletManagementViewModel
            {
                HouseholdMemberId = member.HouseholdMemberId,
                HasHouseholdMembership = true,
                Wallets = walletCards,
                SelectedWalletId = activeWallet?.WalletId,
                SelectedWallet = activeWallet,
                WalletHistories = histories,
                EditWalletId = activeWallet?.WalletId ?? 0,
                EditWalletName = activeWallet?.WalletName ?? string.Empty,
                EditLimitAmount = activeWallet?.LimitAmount
            };
        }

        // Xây dựng danh sách Lịch sử biến động Ví kèm tính toán 
        private static List<WalletHistoryItemViewModel> BuildWalletHistory(
            WalletCardItemViewModel wallet,
            List<Income> walletIncomes,
            List<Expense> walletExpenses)
        {
            var rawEvents = new List<WalletHistoryItemViewModel>();

            foreach (var inc in walletIncomes)
            {
                bool isTransfer = inc.Description != null &&
                                  (inc.Description.Contains("Chuyển", StringComparison.OrdinalIgnoreCase) ||
                                   inc.Description.Contains("Transfer", StringComparison.OrdinalIgnoreCase));

                rawEvents.Add(new WalletHistoryItemViewModel
                {
                    RecordId = inc.IncomeId,
                    TransactionDate = inc.IncomeDate,
                    DisplayTime = $"{inc.IncomeDate:dd/MM/yyyy} 08:00",
                    ChangeType = isTransfer ? "Transfer" : "Income",
                    ChangeTypeLabel = isTransfer ? "Chuyển tiền" : "Thu nhập",
                    CategoryOrTitle = isTransfer ? "Nhận chuyển khoản" : "Khoản thu",
                    DetailDescription = string.IsNullOrWhiteSpace(inc.Description)
                        ? "Ghi nhận khoản thu nhập"
                        : inc.Description,
                    SignedAmount = inc.Amount
                });
            }

            foreach (var exp in walletExpenses)
            {
                string catName = exp.Category == null
                    ? "[Category Deleted]"
                    : (string.Equals(exp.Category.Status, "Inactive", StringComparison.OrdinalIgnoreCase)
                        ? $"{exp.Category.CategoryName} [Category Deleted]"
                        : exp.Category.CategoryName);

                bool isTransfer = exp.Description != null &&
                                  (exp.Description.Contains("Chuyển", StringComparison.OrdinalIgnoreCase) ||
                                   exp.Description.Contains("Transfer", StringComparison.OrdinalIgnoreCase));

                string descText = string.IsNullOrWhiteSpace(exp.Description)
                    ? catName
                    : $"{catName}: {exp.Description}";

                rawEvents.Add(new WalletHistoryItemViewModel
                {
                    RecordId = exp.ExpenseId,
                    TransactionDate = exp.ExpenseDate,
                    DisplayTime = $"{exp.ExpenseDate:dd/MM/yyyy} 15:42",
                    ChangeType = isTransfer ? "Transfer" : "Expense",
                    ChangeTypeLabel = isTransfer ? "Chuyển tiền" : "Chi tiêu",
                    CategoryOrTitle = catName,
                    DetailDescription = descText,
                    SignedAmount = -exp.Amount
                });
            }

            // Sắp xếp từ mới nhất xuống cũ nhất
            var sortedDesc = rawEvents
                .OrderByDescending(e => e.TransactionDate)
                .ThenByDescending(e => e.RecordId)
                .ToList();

            // Tính toán Số dư sau GD (BalanceAfter) từ số dư hiện tại lùi dần về quá khứ
            decimal runningBalance = wallet.CurrentBalance;
            foreach (var item in sortedDesc)
            {
                item.BalanceAfter = runningBalance;
                runningBalance -= item.SignedAmount;
            }

            // Nếu có sự chênh lệch (Số dư ban đầu / Điều chỉnh số dư thực tế) và danh sách có giao dịch hoặc số dư != 0
            if (runningBalance != 0 && sortedDesc.Any())
            {
                var oldestDate = sortedDesc.Last().TransactionDate;
                sortedDesc.Add(new WalletHistoryItemViewModel
                {
                    RecordId = 0,
                    TransactionDate = oldestDate,
                    DisplayTime = $"{oldestDate:dd/MM/yyyy} 00:00",
                    ChangeType = "Adjustment",
                    ChangeTypeLabel = "Số dư khởi tạo / Đối soát",
                    CategoryOrTitle = "Khởi tạo / Điều chỉnh",
                    DetailDescription = "Số dư ban đầu hoặc đối soát khớp tiền thực tế trong ví",
                    SignedAmount = runningBalance,
                    BalanceAfter = runningBalance
                });
            }
            else if (!sortedDesc.Any() && wallet.CurrentBalance != 0)
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Now);
                sortedDesc.Add(new WalletHistoryItemViewModel
                {
                    RecordId = 0,
                    TransactionDate = today,
                    DisplayTime = $"{today:dd/MM/yyyy} 08:00",
                    ChangeType = "Adjustment",
                    ChangeTypeLabel = "Số dư ban đầu",
                    CategoryOrTitle = "Khởi tạo ví",
                    DetailDescription = $"Khởi tạo ví cá nhân \"{wallet.WalletName}\"",
                    SignedAmount = wallet.CurrentBalance,
                    BalanceAfter = wallet.CurrentBalance
                });
            }

            return sortedDesc;
        }

        // =========================================
        // UC: CREATE PERSONAL WALLET (TẠO VÍ CÁ NHÂN)
        // =========================================
        public async Task<(bool Success, string Message, int? CreatedWalletId)> CreateWalletAsync(
            int userId,
            string walletName,
            decimal initialBalance,
            decimal? limitAmount)
        {
            if (string.IsNullOrWhiteSpace(walletName))
            {
                return (false, "Vui lòng nhập tên ví cá nhân.", null);
            }

            if (initialBalance <= 0)
            {
                return (false, "Số dư ban đầu phải lớn hơn 0 VNĐ.", null);
            }

            if (!limitAmount.HasValue || limitAmount.Value <= 0)
            {
                return (false, "Hạn mức chi tiêu tháng phải lớn hơn 0 VNĐ.", null);
            }

            var member = await _walletRepository.GetMemberByUserIdAsync(userId);
            if (member == null)
            {
                return (false, "Bạn cần tạo hoặc tham gia một Hộ gia đình trước khi tạo ví cá nhân.", null);
            }

            string trimmedName = walletName.Trim();
            var existingWallets = await _walletRepository.GetWalletsByMemberIdAsync(member.HouseholdMemberId);

            bool isDuplicate = existingWallets.Any(w =>
                string.Equals(w.WalletName.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                return (false, $"Bạn đã có một ví cá nhân mang tên \"{trimmedName}\". Vui lòng chọn tên khác.", null);
            }

            var newWallet = new Wallet
            {
                HouseholdMemberId = member.HouseholdMemberId,
                WalletName = trimmedName,
                Amount = initialBalance,
                LimitAmount = limitAmount
            };

            var savedWallet = await _walletRepository.AddAsync(newWallet);

            string limitInfo = savedWallet.LimitAmount.HasValue
                ? $" (Hạn mức chi tiêu: {savedWallet.LimitAmount.Value:N0} VNĐ/tháng)"
                : "";

            return (
                true,
                $"Tạo ví cá nhân \"{savedWallet.WalletName}\" thành công với số dư {savedWallet.Amount:N0} VNĐ{limitInfo}!",
                savedWallet.WalletId
            );
        }

        // =========================================
        // UC: WALLET SPENDING LIMIT & UPDATE WALLET
        // =========================================
        public async Task<(bool Success, string Message)> UpdateWalletSettingsAsync(
            int userId,
            int walletId,
            string walletName,
            decimal? limitAmount)
        {
            if (string.IsNullOrWhiteSpace(walletName))
            {
                return (false, "Tên ví cá nhân không được để trống.");
            }

            if (!limitAmount.HasValue || limitAmount.Value <= 0)
            {
                return (false, "Hạn mức chi tiêu tháng phải lớn hơn 0 VNĐ.");
            }

            var member = await _walletRepository.GetMemberByUserIdAsync(userId);
            if (member == null)
            {
                return (false, "Không tìm thấy thông tin thành viên hợp lệ.");
            }

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null || wallet.HouseholdMemberId != member.HouseholdMemberId)
            {
                return (false, "Không tìm thấy ví cá nhân hoặc bạn không phải chủ sở hữu của ví này.");
            }

            string trimmedName = walletName.Trim();
            var existingWallets = await _walletRepository.GetWalletsByMemberIdAsync(member.HouseholdMemberId);

            bool isDuplicate = existingWallets.Any(w =>
                w.WalletId != walletId &&
                string.Equals(w.WalletName.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                return (false, $"Tên ví \"{trimmedName}\" đã tồn tại trong danh sách ví của bạn.");
            }

            wallet.WalletName = trimmedName;
            wallet.LimitAmount = limitAmount;

            await _walletRepository.UpdateAsync(wallet);

            // Kiểm tra tổng chi tiêu trong tháng để phát cảnh báo vượt hạn mức hoặc cảnh báo số dư âm (Wallet Spending Limit / BR-18)
            var expenses = await _walletRepository.GetExpensesByMemberIdAsync(member.HouseholdMemberId);
            var walletExpenses = expenses.Where(e => e.WalletId == wallet.WalletId).ToList();

            DateOnly nowDate = DateOnly.FromDateTime(DateTime.Now);
            decimal monthlyExpense = walletExpenses
                .Where(e => e.ExpenseDate.Year == nowDate.Year && e.ExpenseDate.Month == nowDate.Month)
                .Sum(e => e.Amount);

            if (monthlyExpense == 0 && walletExpenses.Any())
            {
                var latestMonth = walletExpenses
                    .OrderByDescending(e => e.ExpenseDate)
                    .Select(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                    .FirstOrDefault();

                if (latestMonth != null)
                {
                    monthlyExpense = walletExpenses
                        .Where(e => e.ExpenseDate.Year == latestMonth.Year && e.ExpenseDate.Month == latestMonth.Month)
                        .Sum(e => e.Amount);
                }
            }

            bool isNegative = wallet.Amount < 0;
            bool isOverLimit = wallet.LimitAmount.HasValue && wallet.LimitAmount.Value > 0 && monthlyExpense > wallet.LimitAmount.Value;

            if (isNegative && isOverLimit)
            {
                await _walletRepository.AddNotificationAsync(new Notification
                {
                    UserId = userId,
                    Message = $"[Cảnh báo Kép - Ví {wallet.WalletName}] Số dư ví đang âm ({wallet.Amount:N0} VNĐ) và tổng chi tiêu tháng ({monthlyExpense:N0} VNĐ) đã vượt hạn mức ({wallet.LimitAmount!.Value:N0} VNĐ)!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else if (isNegative)
            {
                await _walletRepository.AddNotificationAsync(new Notification
                {
                    UserId = userId,
                    Message = $"[Cảnh báo Số dư âm - BR-18] Ví cá nhân \"{wallet.WalletName}\" hiện có số dư âm ({wallet.Amount:N0} VNĐ).",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else if (isOverLimit)
            {
                await _walletRepository.AddNotificationAsync(new Notification
                {
                    UserId = userId,
                    Message = $"[Cảnh báo Vượt hạn mức chi tiêu] Tổng chi tiêu trên ví \"{wallet.WalletName}\" ({monthlyExpense:N0} VNĐ) đã vượt hạn mức tháng ({wallet.LimitAmount!.Value:N0} VNĐ).",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            return (true, $"Đã cập nhật thông tin và hạn mức chi tiêu cho \"{wallet.WalletName}\" thành công!");
        }
    }
}

