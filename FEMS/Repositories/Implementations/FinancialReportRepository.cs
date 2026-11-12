
using FEMS.Data;
using FEMS.Models.Entities;
using FEMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEMS.Repositories.Implementations
{
    public class FinancialReportRepository : IFinancialReportRepository
    {
        private readonly FemsDbContext _context;

        public FinancialReportRepository(FemsDbContext context)
        {
            _context = context;
        }

        public async Task<List<Income>> GetIncomesAsync(
            List<int> memberIds, DateOnly startDate, DateOnly endDate)
        {
            return await _context.Incomes
                .AsNoTracking()
                .Include(i => i.Wallet)
                .Include(i => i.HouseholdMember)
                    .ThenInclude(m => m.User)
                .Where(i => memberIds.Contains(i.HouseholdMemberId)
                    && i.IncomeDate >= startDate
                    && i.IncomeDate < endDate)
                .ToListAsync();
        }

        public async Task<List<Expense>> GetExpensesAsync(
            List<int> memberIds, DateOnly startDate, DateOnly endDate)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Wallet)
                .Include(e => e.HouseholdMember)
                    .ThenInclude(m => m.User)
                .Where(e => memberIds.Contains(e.HouseholdMemberId)
                    && e.ExpenseDate >= startDate
                    && e.ExpenseDate < endDate)
                .ToListAsync();
        }
    }
}
