using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IWeeklyPlanRepository : IGenericRepository<WeeklyPlan>
    {
        Task<WeeklyPlan?> GetWithDetailsAsync(int id);
        Task<List<WeeklyPlan>> GetByDateRangeAsync(DateTime weekStart, DateTime weekEnd);
        Task<WeeklyPlan?> GetByWeekAsync(int year, int month, int weekNumber, int monthlyPlanId);
        Task<List<WeeklyPlan>> GetByMonthlyPlanIdAsync(int monthlyPlanId);
    }
}

