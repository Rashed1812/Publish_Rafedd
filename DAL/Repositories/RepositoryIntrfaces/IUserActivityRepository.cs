using DAL.Data.Models.NotificationsLogs;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IUserActivityRepository : IGenericRepository<UserActivity>
    {
        Task<List<UserActivity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50);
        Task<List<UserActivity>> GetAllWithUsersAsync(int skip = 0, int take = 50);
        Task<int> GetTotalCountAsync();
    }
}

