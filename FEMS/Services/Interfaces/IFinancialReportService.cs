
using FEMS.ViewModels.FinancialReport;

namespace FEMS.Services.Interfaces
{
    public interface IFinancialReportService
    {
        Task<FinancialSummaryViewModel?> GetSummaryAsync(
            int userId, int months);
        Task<DetailedReportViewModel?> GetDetailedReportAsync(
    int userId, int months);
    }
}
