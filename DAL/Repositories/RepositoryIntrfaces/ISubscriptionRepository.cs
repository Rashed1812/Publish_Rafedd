using DAL.Data.Models.Subscription;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface ISubscriptionRepository : IGenericRepository<Subscription>
    {
        Task<Subscription?> GetByManagerIdAsync(int managerId);
        Task<List<Subscription>> GetAllWithDetailsAsync(bool? isActive = null);
        Task<int> GetActiveSubscriptionsCountAsync();
        IQueryable<Subscription> GetFilteredQueryable();
    }
}

