using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Employee?> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);
        }

        public async Task<Employee?> GetWithDetailsAsync(string userId)
        {
            return await _dbSet
                .Include(e => e.User)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);
        }

        public async Task<List<Employee>> GetByManagerAsync(string managerUserId)
        {
            return await _dbSet
                .Include(e => e.User)
                .Where(e => e.ManagerUserId == managerUserId && e.IsActive)
                .ToListAsync();
        }

        public async Task<int> GetEmployeeCountByManagerAsync(string managerUserId)
        {
            return await _dbSet
                .CountAsync(e => e.ManagerUserId == managerUserId && e.IsActive);
        }

        // NEW: For flexible filtering
        public IQueryable<Employee> GetFilteredQueryable(string? managerUserId = null)
        {
            var query = _dbSet
                .Include(e => e.User)
                .Include(e => e.Manager)
                .AsQueryable();

            // Optional manager filter
            if (!string.IsNullOrEmpty(managerUserId))
            {
                query = query.Where(e => e.ManagerUserId == managerUserId);
            }

            return query;
        }
    }
}

