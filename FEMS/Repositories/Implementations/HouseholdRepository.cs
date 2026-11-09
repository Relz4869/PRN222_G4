using FEMS.Data;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Repositories.Implementations
{
    public class HouseholdRepository : IHouseholdRepository
    {
        private readonly FemsDbContext _context;

        public HouseholdRepository(FemsDbContext context)
        {
            _context = context;
        }

        public async Task<HouseholdMember?> GetActiveMemberByUserIdAsync(int userId)
        {
            return await _context.HouseholdMembers
                .Include(m => m.Household)
                .Include(m => m.User)
                .FirstOrDefaultAsync(m =>
                    m.UserId == userId &&
                    m.Status == "Active" &&
                    m.Household.Status == "Active");
        }

        public async Task<HouseholdMember?> GetMemberByIdAsync(int householdMemberId)
        {
            return await _context.HouseholdMembers
                .Include(m => m.Household)
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.HouseholdMemberId == householdMemberId);
        }

        public async Task<HouseholdMember?> GetMemberByHouseholdAndUserAsync(int householdId, int userId)
        {
            return await _context.HouseholdMembers
                .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.UserId == userId);
        }

        public async Task<Household?> GetHouseholdByIdAsync(int householdId)
        {
            return await _context.Households
                .FirstOrDefaultAsync(h => h.HouseholdId == householdId && h.Status == "Active");
        }

        public async Task<List<HouseholdMember>> GetActiveMembersByHouseholdIdAsync(int householdId)
        {
            return await _context.HouseholdMembers
                .Include(m => m.User)
                .Where(m => m.HouseholdId == householdId && m.Status == "Active")
                .OrderByDescending(m => m.Role == "Manager")
                .ThenBy(m => m.JoinedAt)
                .ToListAsync();
        }

        public async Task<Household> AddHouseholdAsync(Household household)
        {
            await _context.Households.AddAsync(household);
            await _context.SaveChangesAsync();
            return household;
        }

        public async Task UpdateHouseholdAsync(Household household)
        {
            _context.Households.Update(household);
            await _context.SaveChangesAsync();
        }

        public async Task AddMemberAsync(HouseholdMember member)
        {
            await _context.HouseholdMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(HouseholdMember member)
        {
            _context.HouseholdMembers.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMembersAsync(IEnumerable<HouseholdMember> members)
        {
            _context.HouseholdMembers.UpdateRange(members);
            await _context.SaveChangesAsync();
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadInviteNotificationsByHouseholdIdAsync(int householdId)
        {
            string prefix = $"[INVITE:{householdId}:";
            return await _context.Notifications
                .Include(n => n.User)
                .Where(n => !n.IsRead && n.Message.StartsWith(prefix))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetNotificationByIdAsync(int notificationId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }

        public async Task UpdateNotificationAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Wallet>> GetWalletsByMemberIdAsync(int householdMemberId)
        {
            return await _context.Wallets
                .AsNoTracking()
                .Where(w => w.HouseholdMemberId == householdMemberId)
                .OrderBy(w => w.WalletId)
                .ToListAsync();
        }

        public async Task<List<Income>> GetIncomesByMemberIdAsync(int householdMemberId)
        {
            return await _context.Incomes
                .AsNoTracking()
                .Include(i => i.Wallet)
                .Where(i => i.HouseholdMemberId == householdMemberId)
                .OrderByDescending(i => i.IncomeDate)
                .ThenByDescending(i => i.IncomeId)
                .ToListAsync();
        }

        public async Task<List<Expense>> GetExpensesByMemberIdAsync(int householdMemberId)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Wallet)
                .Include(e => e.Category)
                .Where(e => e.HouseholdMemberId == householdMemberId)
                .OrderByDescending(e => e.ExpenseDate)
                .ThenByDescending(e => e.ExpenseId)
                .ToListAsync();
        }
    }
}

