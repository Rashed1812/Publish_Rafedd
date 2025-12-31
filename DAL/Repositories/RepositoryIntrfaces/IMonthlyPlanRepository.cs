using DAL.Data.Models.AIPlanning;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IMonthlyPlanRepository : IGenericRepository<MonthlyPlan>
    {
        Task<MonthlyPlan?> GetByAnnualTargetAndMonthAsync(int annualTargetId, int month);
        Task<List<MonthlyPlan>> GetByAnnualTargetAsync(int annualTargetId);
    }
}

