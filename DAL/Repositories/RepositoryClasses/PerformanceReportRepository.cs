using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class PerformanceReportRepository : GenericRepository<PerformanceReport>, IPerformanceReportRepository
    {
        public PerformanceReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PerformanceReport?> GetByWeeklyPlanAsync(int weeklyPlanId)
        {
            return await _dbSet
                .Include(pr => pr.WeeklyPlan)
                .FirstOrDefaultAsync(pr => pr.WeeklyPlanId == weeklyPlanId);
        }

        public async Task<List<PerformanceReport>> GetByManagerAndYearAsync(string managerUserId, int year)
        {
            return await _dbSet
                .Include(pr => pr.WeeklyPlan)
                    .ThenInclude(wp => wp.MonthlyPlan)
                        .ThenInclude(mp => mp.AnnualTarget)
                .Where(pr => pr.WeeklyPlan.Year == year &&
                           pr.WeeklyPlan.MonthlyPlan.AnnualTarget.ManagerUserId == managerUserId)
                .OrderByDescending(pr => pr.WeeklyPlan.Year)
                    .ThenByDescending(pr => pr.WeeklyPlan.Month)
                    .ThenByDescending(pr => pr.WeeklyPlan.WeekNumber)
                .ToListAsync();
        }
    }
}

