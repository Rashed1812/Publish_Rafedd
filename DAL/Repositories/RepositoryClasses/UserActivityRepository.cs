using DAL.Data;
using DAL.Data.Models.NotificationsLogs;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class UserActivityRepository : GenericRepository<UserActivity>, IUserActivityRepository
    {
        public UserActivityRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<UserActivity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50)
        {
            return await _dbSet
                .Include(ua => ua.User)
                .Where(ua => ua.UserId == userId)
                .OrderByDescending(ua => ua.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<UserActivity>> GetAllWithUsersAsync(int skip = 0, int take = 50)
        {
            return await _dbSet
                .Include(ua => ua.User)
                .OrderByDescending(ua => ua.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _dbSet.CountAsync();
        }
    }
}

