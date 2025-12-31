using DAL.Data;
using DAL.Data.Models.Subscription;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            return await _dbSet
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Manager)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<List<Payment>> GetAllWithDetailsAsync(string? status = null)
        {
            var query = _dbSet
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Manager)
                        .ThenInclude(m => m.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            return await query
                .OrderByDescending(p => p.PaidAt ?? DateTime.MinValue) // Order by PaidAt if available, otherwise by creation order
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _dbSet
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
        }

        public async Task<decimal> GetMonthlyRevenueAsync(DateTime startDate)
        {
            return await _dbSet
                .Where(p => p.Status == "Completed" && p.PaidAt.HasValue && p.PaidAt.Value >= startDate)
                .SumAsync(p => p.Amount);
        }

        public async Task<List<Payment>> GetRevenueStatisticsAsync()
        {
            return await _dbSet
                .Include(p => p.Subscription)
                .ToListAsync();
        }
    }
}

