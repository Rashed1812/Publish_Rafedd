using DAL.Data;
using DAL.Data.Models.Subscription;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<Subscription?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .Include(s => s.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Subscription?> GetByManagerIdAsync(int managerId)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .Include(s => s.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(s => s.ManagerId == managerId && s.IsActive);
        }

        public async Task<List<Subscription>> GetAllWithDetailsAsync(bool? isActive = null)
        {
            var query = _dbSet
                .Include(s => s.Plan)
                .Include(s => s.Manager)
                    .ThenInclude(m => m.User)
                .AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            return await query
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<int> GetActiveSubscriptionsCountAsync()
        {
            return await _dbSet
                .CountAsync(s => s.IsActive);
        }

        public IQueryable<Subscription> GetFilteredQueryable()
        {
            return _dbSet
                .Include(s => s.Plan)
                .Include(s => s.Manager)
                    .ThenInclude(m => m.User)
                .AsQueryable();
        }
    }
}

