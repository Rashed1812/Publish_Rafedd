using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class WeeklyPlanRepository : GenericRepository<WeeklyPlan>, IWeeklyPlanRepository
    {
        public WeeklyPlanRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WeeklyPlan?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(wp => wp.MonthlyPlan)
                    .ThenInclude(mp => mp.AnnualTarget)
                .Include(wp => wp.PerformanceReport)
                .FirstOrDefaultAsync(wp => wp.Id == id);
        }

        public async Task<List<WeeklyPlan>> GetByDateRangeAsync(DateTime weekStart, DateTime weekEnd)
        {
            return await _dbSet
                .Include(wp => wp.MonthlyPlan)
                    .ThenInclude(mp => mp.AnnualTarget)
                .Where(wp => wp.WeekStartDate <= weekEnd && wp.WeekEndDate >= weekStart)
                .ToListAsync();
        }

        public async Task<WeeklyPlan?> GetByWeekAsync(int year, int month, int weekNumber, int monthlyPlanId)
        {
            return await _dbSet
                .Include(wp => wp.MonthlyPlan)
                .FirstOrDefaultAsync(wp => wp.Year == year &&
                                          wp.Month == month &&
                                          wp.WeekNumber == weekNumber &&
                                          wp.MonthlyPlanId == monthlyPlanId);
        }

        public async Task<List<WeeklyPlan>> GetByMonthlyPlanIdAsync(int monthlyPlanId)
        {
            return await _dbSet
                .Include(wp => wp.MonthlyPlan)
                .Include(wp => wp.PerformanceReport)
                .Where(wp => wp.MonthlyPlanId == monthlyPlanId)
                .OrderBy(wp => wp.WeekNumber)
                .ToListAsync();
        }
    }
}

