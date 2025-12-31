using System.Linq.Expressions;
using BLL.ServiceAbstraction;
using DAL.Data.Models.IdentityModels;
using DAL.Extensions;
using DAL.Repositories.RepositoryIntrfaces;
using Shared.DTOS.Common;
using Shared.DTOS.Users;

namespace BLL.Service
{
    public class ManagerService : IManagerService
    {
        private readonly IManagerRepository _managerRepository;

        public ManagerService(IManagerRepository managerRepository)
        {
            _managerRepository = managerRepository;
        }

        public async Task<PagedResponse<ManagerDto>> GetManagersAsync(ManagerFilterParams filterParams)
        {
            var query = _managerRepository.GetFilteredQueryable();

            // Apply optional filters
            if (filterParams.IsActive.HasValue)
            {
                query = query.Where(m => m.IsActive == filterParams.IsActive.Value);
            }

            if (filterParams.HasActiveSubscription.HasValue)
            {
                if (filterParams.HasActiveSubscription.Value)
                {
                    query = query.Where(m => m.Subscription != null && m.Subscription.IsActive);
                }
                else
                {
                    query = query.Where(m => m.Subscription == null || !m.Subscription.IsActive);
                }
            }

            // Apply subscription end date range
            if (filterParams.SubscriptionEndDateFrom.HasValue)
            {
                query = query.Where(m => m.SubscriptionEndsAt >= filterParams.SubscriptionEndDateFrom.Value);
            }

            if (filterParams.SubscriptionEndDateTo.HasValue)
            {
                query = query.Where(m => m.SubscriptionEndsAt <= filterParams.SubscriptionEndDateTo.Value);
            }

            // Apply optional search
            if (!string.IsNullOrEmpty(filterParams.Search))
            {
                var searchLower = filterParams.Search.ToLower();
                query = query.Where(m =>
                    m.User.FullName.ToLower().Contains(searchLower) ||
                    m.User.Email.ToLower().Contains(searchLower) ||
                    (m.User.PhoneNumber != null && m.User.PhoneNumber.Contains(searchLower)) ||
                    m.CompanyName.ToLower().Contains(searchLower));
            }

            // Define sortable fields
            var sortExpressions = new Dictionary<string, Expression<Func<Manager, object>>>
            {
                ["name"] = m => m.User.FullName,
                ["companyname"] = m => m.CompanyName,
                ["createdat"] = m => m.CreatedAt,
                ["subscriptionendsat"] = m => m.SubscriptionEndsAt ?? DateTime.MinValue
            };

            // Apply sorting with default (createdAt desc)
            query = query.ApplySorting(
                filterParams.SortBy,
                filterParams.IsDescending,
                sortExpressions,
                defaultSort: m => m.CreatedAt);

            // Get paged results
            var (items, totalCount) = await query.ToPagedListAsync(
                filterParams.GetPage(),
                filterParams.GetPageSize());

            // Map to DTOs
            var dtos = items.Select(m => new ManagerDto
            {
                Id = m.UserId,
                Name = m.User.FullName,
                Email = m.User.Email,
                Phone = m.User.PhoneNumber ?? "",
                CompanyName = m.CompanyName,
                BusinessType = m.BusinessType,
                BusinessDescription = m.BusinessDescription,
                IsActive = m.IsActive,
                EmployeeCount = m.Employees.Count(e => e.IsActive),
                CreatedAt = m.CreatedAt,
                Subscription = m.Subscription != null ? new SubscriptionInfoDto
                {
                    Id = m.Subscription.Id,
                    PlanName = m.Subscription.Plan?.Name ?? "",
                    IsActive = m.Subscription.IsActive,
                    StartDate = m.Subscription.StartDate,
                    EndDate = m.Subscription.EndDate,
                    AutoRenew = m.Subscription.AutoRenew
                } : null
            }).ToList();

            return PagedResponse<ManagerDto>.Create(
                dtos,
                totalCount,
                filterParams.GetPage(),
                filterParams.GetPageSize());
        }
    }
}
