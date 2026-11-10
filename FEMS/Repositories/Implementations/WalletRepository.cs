using FEMS.Data;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Repositories.Implementations
{
    public class WalletRepository : IWalletRepository
    {
        private readonly FemsDbContext _context;

        public WalletRepository(FemsDbContext context)
        {
            _context = context;
        }

        public async Task<HouseholdMember?> GetMemberByUserIdAsync(int userId)
        {
            // Ưu tiên lấy bản ghi HouseholdMember đang Active, nếu không có thì lấy bản ghi gần nhất của User
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

        public async Task<Wallet?> GetByIdAsync(int walletId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.WalletId == walletId);
        }

        public async Task<Wallet> AddAsync(Wallet wallet)
        {
            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        public async Task UpdateAsync(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Wallet wallet)
        {
            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();
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

        public async Task AddNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
    }
}

