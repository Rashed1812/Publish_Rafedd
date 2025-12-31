using Shared.DTOS.Performance;

namespace BLL.ServiceAbstraction
{
    public interface IPerformanceAnalysisService
    {
        Task<PerformanceReportDto> GenerateWeeklyPerformanceReportAsync(int weeklyPlanId);
        Task<PerformanceReportDto?> GetPerformanceReportAsync(int weeklyPlanId);
        Task<List<PerformanceReportDto>> GetPerformanceReportsByYearAsync(string managerUserId, int year);
    }
}

