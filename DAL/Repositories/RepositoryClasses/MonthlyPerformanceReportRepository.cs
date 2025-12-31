using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class MonthlyPerformanceReportRepository : IMonthlyPerformanceReportRepository
    {
        private readonly ApplicationDbContext _context;

        public MonthlyPerformanceReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MonthlyPerformanceReport?> GetByIdAsync(int id)
        {
            return await _context.MonthlyPerformanceReports
                .Include(mpr => mpr.MonthlyPlan)
                    .ThenInclude(mp => mp.AnnualTarget)
                .FirstOrDefaultAsync(mpr => mpr.Id == id);
        }

        public async Task<MonthlyPerformanceReport?> GetByMonthlyPlanIdAsync(int monthlyPlanId)
        {
            return await _context.MonthlyPerformanceReports
                .Include(mpr => mpr.MonthlyPlan)
                    .ThenInclude(mp => mp.AnnualTarget)
                .FirstOrDefaultAsync(mpr => mpr.MonthlyPlanId == monthlyPlanId);
        }

        public async Task<List<MonthlyPerformanceReport>> GetByManagerUserIdAndYearAsync(string managerUserId, int year)
        {
            return await _context.MonthlyPerformanceReports
                .Include(mpr => mpr.MonthlyPlan)
                    .ThenInclude(mp => mp.AnnualTarget)
                .Where(mpr => mpr.MonthlyPlan.AnnualTarget.ManagerUserId == managerUserId
                           && mpr.MonthlyPlan.Year == year)
                .OrderBy(mpr => mpr.MonthlyPlan.Month)
                .ToListAsync();
        }

        public async Task<MonthlyPerformanceReport> CreateAsync(MonthlyPerformanceReport report)
        {
            _context.MonthlyPerformanceReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<bool> ExistsForMonthlyPlanAsync(int monthlyPlanId)
        {
            return await _context.MonthlyPerformanceReports
                .AnyAsync(mpr => mpr.MonthlyPlanId == monthlyPlanId);
        }
    }
}
