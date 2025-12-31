using Shared.DTOS.Performance;

namespace BLL.ServiceAbstraction
{
    public interface IMonthlyPerformanceAnalysisService
    {
        Task<MonthlyPerformanceReportDto> GenerateMonthlyPerformanceReportAsync(int monthlyPlanId);
        Task<MonthlyPerformanceReportDto?> GetMonthlyReportAsync(int monthlyPlanId);
        Task<List<MonthlyPerformanceReportDto>> GetMonthlyReportsByYearAsync(string managerUserId, int year);
    }
}
