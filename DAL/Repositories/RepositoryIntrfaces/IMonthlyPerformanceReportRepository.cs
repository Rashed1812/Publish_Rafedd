using DAL.Data.Models.AIPlanning;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IMonthlyPerformanceReportRepository
    {
        Task<MonthlyPerformanceReport?> GetByIdAsync(int id);
        Task<MonthlyPerformanceReport?> GetByMonthlyPlanIdAsync(int monthlyPlanId);
        Task<List<MonthlyPerformanceReport>> GetByManagerUserIdAndYearAsync(string managerUserId, int year);
        Task<MonthlyPerformanceReport> CreateAsync(MonthlyPerformanceReport report);
        Task<bool> ExistsForMonthlyPlanAsync(int monthlyPlanId);
    }
}
