using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IPerformanceReportRepository : IGenericRepository<PerformanceReport>
    {
        Task<PerformanceReport?> GetByWeeklyPlanAsync(int weeklyPlanId);
        Task<List<PerformanceReport>> GetByManagerAndYearAsync(string managerUserId, int year);
    }
}

