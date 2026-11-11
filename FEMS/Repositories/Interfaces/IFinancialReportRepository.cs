
using FEMS.Models.Entities;

namespace FEMS.Repositories.Interfaces
{
    public interface IFinancialReportRepository
    {
        Task<List<Income>> GetIncomesAsync(
            List<int> memberIds, DateOnly startDate, DateOnly endDate);

        Task<List<Expense>> GetExpensesAsync(
            List<int> memberIds, DateOnly startDate, DateOnly endDate);
    }
}
