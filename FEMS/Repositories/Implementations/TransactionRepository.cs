using FEMS.Data;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Repositories.Implementations
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly FemsDbContext _context;

        public TransactionRepository(FemsDbContext context)
        {
            _context = context;
        }

        public async Task<HouseholdMember?> GetMemberByUserIdAsync(int userId)
        {
            return await _context.HouseholdMembers
                .Include(m => m.User)
                .Include(m => m.Household)
                .OrderByDescending(m => m.Status == "Active")
                .ThenByDescending(m => m.JoinedAt)
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<List<Wallet>> GetWalletsByMemberIdAsync(int memberId)
        {
            return await _context.Wallets
                .Where(w => w.HouseholdMemberId == memberId)
                .OrderBy(w => w.WalletId)
                .ToListAsync();
        }

        public async Task<Wallet?> GetWalletByIdAsync(int walletId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.WalletId == walletId);
        }

        public async Task UpdateWalletAsync(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Category>> GetActiveCategoriesByMemberIdAsync(int memberId)
        {
            return await _context.Categories
                .Where(c => c.HouseholdMemberId == memberId && c.Status == "Active")
                .OrderBy(c => c.CategoryId)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task<List<Income>> GetIncomesByMemberIdAsync(int memberId)
        {
            return await _context.Incomes
                .Include(i => i.Wallet)
                .Where(i => i.HouseholdMemberId == memberId)
                .OrderByDescending(i => i.IncomeDate)
                .ThenByDescending(i => i.IncomeId)
                .ToListAsync();
        }

        public async Task<List<Expense>> GetExpensesByMemberIdAsync(int memberId)
        {
            return await _context.Expenses
                .Include(e => e.Wallet)
                .Include(e => e.Category)
                .Where(e => e.HouseholdMemberId == memberId)
                .OrderByDescending(e => e.ExpenseDate)
                .ThenByDescending(e => e.ExpenseId)
                .ToListAsync();
        }

        public async Task<Income> AddIncomeAsync(Income income)
        {
            await _context.Incomes.AddAsync(income);
            await _context.SaveChangesAsync();
            return income;
        }

        public async Task<Expense> AddExpenseAsync(Expense expense)
        {
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
            return expense;
        }

        public async Task<List<HouseholdMember>> GetActiveManagersByHouseholdIdAsync(int householdId)
        {
            return await _context.HouseholdMembers
                .Include(m => m.User)
                .Where(m => m.HouseholdId == householdId && m.Status == "Active" && m.Role == "Manager")
                .ToListAsync();
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
    }
}

