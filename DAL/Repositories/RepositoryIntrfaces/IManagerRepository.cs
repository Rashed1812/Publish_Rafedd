using DAL.Data.Models.IdentityModels;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IManagerRepository : IGenericRepository<Manager>
    {
        Task<Manager?> GetByUserIdAsync(string userId);
        Task<Manager?> GetWithDetailsAsync(string userId);
        Task<List<Manager>> GetAllActiveAsync();

        IQueryable<Manager> GetFilteredQueryable();
    }
}

