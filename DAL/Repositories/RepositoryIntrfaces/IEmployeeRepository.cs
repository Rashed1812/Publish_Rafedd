using DAL.Data.Models.IdentityModels;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetByUserIdAsync(string userId);
        Task<Employee?> GetWithDetailsAsync(string userId);
        Task<List<Employee>> GetByManagerAsync(string managerUserId);
        Task<int> GetEmployeeCountByManagerAsync(string managerUserId);

        // NEW: For flexible filtering
        IQueryable<Employee> GetFilteredQueryable(string? managerUserId = null);
    }
}

