using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class MonthlyPlanRepository : GenericRepository<MonthlyPlan>, IMonthlyPlanRepository
    {
        public MonthlyPlanRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<MonthlyPlan?> GetByAnnualTargetAndMonthAsync(int annualTargetId, int month)
        {
            return await _dbSet
                .Include(mp => mp.WeeklyPlans)
                .FirstOrDefaultAsync(mp => mp.AnnualTargetId == annualTargetId && mp.Month == month);
        }

        public async Task<List<MonthlyPlan>> GetByAnnualTargetAsync(int annualTargetId)
        {
            return await _dbSet
                .Include(mp => mp.WeeklyPlans)
                .Where(mp => mp.AnnualTargetId == annualTargetId)
                .OrderBy(mp => mp.Month)
                .ToListAsync();
        }
    }
}

