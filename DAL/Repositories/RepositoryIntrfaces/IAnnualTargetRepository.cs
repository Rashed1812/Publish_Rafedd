using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IAnnualTargetRepository : IGenericRepository<AnnualTarget>
    {
        Task<AnnualTarget?> GetByManagerAndYearAsync(string managerUserId, int year);
        Task<List<AnnualTarget>> GetAllByManagerAsync(string managerUserId);
        Task<AnnualTarget?> GetWithDetailsAsync(int id);
    }
}

