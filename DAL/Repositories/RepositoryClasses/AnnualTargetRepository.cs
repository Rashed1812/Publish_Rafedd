using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class AnnualTargetRepository : GenericRepository<AnnualTarget>, IAnnualTargetRepository
    {
        public AnnualTargetRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AnnualTarget?> GetByManagerAndYearAsync(string managerUserId, int year)
        {
            return await _dbSet
                .Include(at => at.MonthlyPlans)
                    .ThenInclude(mp => mp.WeeklyPlans)
                .FirstOrDefaultAsync(at => at.ManagerUserId == managerUserId && at.Year == year);
        }

        public async Task<List<AnnualTarget>> GetAllByManagerAsync(string managerUserId)
        {
            return await _dbSet
                .Include(at => at.MonthlyPlans)
                    .ThenInclude(mp => mp.WeeklyPlans)
                .Where(at => at.ManagerUserId == managerUserId)
                .OrderByDescending(at => at.Year)
                .ToListAsync();
        }

        public async Task<AnnualTarget?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(at => at.MonthlyPlans)
                    .ThenInclude(mp => mp.WeeklyPlans)
                .FirstOrDefaultAsync(at => at.Id == id);
        }
    }
}

