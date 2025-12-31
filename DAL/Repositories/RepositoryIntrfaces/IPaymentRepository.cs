using DAL.Data.Models.Subscription;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByTransactionIdAsync(string transactionId);
        Task<List<Payment>> GetAllWithDetailsAsync(string? status = null);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetMonthlyRevenueAsync(DateTime startDate);
        Task<List<Payment>> GetRevenueStatisticsAsync();
    }
}

