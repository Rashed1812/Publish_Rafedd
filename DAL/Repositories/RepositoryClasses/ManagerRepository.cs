using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class ManagerRepository : GenericRepository<Manager>, IManagerRepository
    {
        public ManagerRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Manager?> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive);
        }

        public async Task<Manager?> GetWithDetailsAsync(string userId)
        {
            return await _dbSet
                .Include(m => m.User)
                .Include(m => m.Subscription)
                    .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive);
        }

        public async Task<List<Manager>> GetAllActiveAsync()
        {
            return await _dbSet
                .Include(m => m.User)
                .Where(m => m.IsActive)
                .ToListAsync();
        }

        public IQueryable<Manager> GetFilteredQueryable()
        {
            return _dbSet
                .Include(m => m.User)
                .Include(m => m.Subscription)
                    .ThenInclude(s => s!.Plan)
                .Include(m => m.Employees.Where(e => e.IsActive))
                .AsQueryable();
        }
    }
}

