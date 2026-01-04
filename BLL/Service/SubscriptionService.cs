using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Data.Models.Subscription;
using DAL.Extensions;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Common;
using Shared.DTOS.Subscription;
using System.Linq.Expressions;

namespace BLL.Service
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ApplicationDbContext context,
            ISubscriptionRepository subscriptionRepository,
            IManagerRepository managerRepository,
            IEmployeeRepository employeeRepository,
            ILogger<SubscriptionService> logger)
        {
            _context = context;
            _subscriptionRepository = subscriptionRepository;
            _managerRepository = managerRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<SubscriptionDto> CreateSubscriptionAsync(string managerUserId, int subscriptionPlanId)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            // Check if manager already has an active subscription
            var existingSubscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
            if (existingSubscription != null && existingSubscription.IsActive)
            {
                throw new InvalidOperationException("Manager already has an active subscription");
            }

            // Get subscription plan
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == subscriptionPlanId && p.IsActive);
            if (plan == null)
            {
                throw new InvalidOperationException("Subscription plan not found or inactive");
            }

            // Create subscription
            var subscription = new Subscription
            {
                ManagerId = manager.Id,
                SubscriptionPlanId = subscriptionPlanId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = false, // Will be activated after payment
                AutoRenew = true
            };

            await _subscriptionRepository.AddAsync(subscription);
            await _subscriptionRepository.SaveChangesAsync();

            // Update manager subscription
            manager.SubscriptionId = subscription.Id;
            manager.SubscriptionEndsAt = subscription.EndDate;
            _managerRepository.Update(manager);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation("Created subscription {SubscriptionId} for manager {ManagerId}", 
                subscription.Id, manager.Id);

            return MapToSubscriptionDto(subscription);
        }

        public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(string managerUserId)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
            if (subscription == null || !subscription.IsActive)
            {
                return null;
            }

            return MapToSubscriptionDto(subscription);
        }

        public async Task<SubscriptionDto> UpgradeSubscriptionAsync(string managerUserId, int newPlanId)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            var currentSubscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
            if (currentSubscription == null)
            {
                throw new InvalidOperationException("No active subscription found");
            }

            // Get new plan
            var newPlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == newPlanId && p.IsActive);
            if (newPlan == null)
            {
                throw new InvalidOperationException("Subscription plan not found or inactive");
            }

            // Check if downgrading (need to check employee count)
            if (newPlan.MaxEmployees < currentSubscription.Plan!.MaxEmployees)
            {
                var employeeCount = await _employeeRepository.GetEmployeeCountByManagerAsync(managerUserId);
                if (employeeCount > newPlan.MaxEmployees)
                {
                    throw new InvalidOperationException(
                        $"Cannot downgrade: Manager has {employeeCount} employees, but new plan allows only {newPlan.MaxEmployees}");
                }
            }

            // Update subscription
            currentSubscription.SubscriptionPlanId = newPlanId;
            _subscriptionRepository.Update(currentSubscription);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation("Upgraded subscription {SubscriptionId} to plan {PlanId}", 
                currentSubscription.Id, newPlanId);

            return MapToSubscriptionDto(currentSubscription);
        }

        public async Task<bool> CancelSubscriptionAsync(string managerUserId)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
            if (subscription == null)
            {
                return false;
            }

            subscription.AutoRenew = false;
            subscription.IsActive = false;
            _subscriptionRepository.Update(subscription);

            manager.SubscriptionEndsAt = DateTime.UtcNow;
            _managerRepository.Update(manager);

            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation("Cancelled subscription {SubscriptionId} for manager {ManagerId}", 
                subscription.Id, manager.Id);

            return true;
        }

        public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.PricePerMonth)
                .ToListAsync();

            return plans.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                PricePerMonth = p.PricePerMonth,
                MaxEmployees = p.MaxEmployees,
                Description = p.Description
            }).ToList();
        }

        public async Task<bool> CheckEmployeeLimitAsync(string managerUserId, int requestedCount)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            if (manager == null)
            {
                return false;
            }

            var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
            if (subscription == null || !subscription.IsActive)
            {
                return false; // No active subscription
            }

            var currentCount = await _employeeRepository.GetEmployeeCountByManagerAsync(managerUserId);
            var newTotal = currentCount + requestedCount;

            return newTotal <= subscription.Plan!.MaxEmployees;
        }

        public async Task<PagedResponse<SubscriptionDto>> GetSubscriptionsAsync(SubscriptionFilterParams filterParams)
        {
            var query = _subscriptionRepository.GetFilteredQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filterParams.Search))
            {
                var searchLower = filterParams.Search.ToLower();
                query = query.Where(s =>
                    s.Manager.User.FullName.ToLower().Contains(searchLower) ||
                    s.Manager.CompanyName.ToLower().Contains(searchLower) ||
                    (s.Manager.User.Email != null && s.Manager.User.Email.ToLower().Contains(searchLower)));
            }

            if (filterParams.PlanId.HasValue)
            {
                query = query.Where(s => s.SubscriptionPlanId == filterParams.PlanId.Value);
            }

            if (filterParams.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filterParams.IsActive.Value);
            }

            if (filterParams.AutoRenew.HasValue)
            {
                query = query.Where(s => s.AutoRenew == filterParams.AutoRenew.Value);
            }

            if (filterParams.StartDateFrom.HasValue)
            {
                query = query.Where(s => s.StartDate >= filterParams.StartDateFrom.Value);
            }

            if (filterParams.StartDateTo.HasValue)
            {
                query = query.Where(s => s.StartDate <= filterParams.StartDateTo.Value);
            }

            if (filterParams.EndDateFrom.HasValue)
            {
                query = query.Where(s => s.EndDate >= filterParams.EndDateFrom.Value);
            }

            if (filterParams.EndDateTo.HasValue)
            {
                query = query.Where(s => s.EndDate <= filterParams.EndDateTo.Value);
            }

            // Define sortable fields
            var sortExpressions = new Dictionary<string, Expression<Func<Subscription, object>>>
            {
                ["planname"] = s => s.Plan.Name,
                ["startdate"] = s => s.StartDate,
                ["enddate"] = s => s.EndDate,
                ["managername"] = s => s.Manager.User.FullName,
                ["isactive"] = s => s.IsActive
            };

            // Apply sorting with default (startDate desc)
            query = query.ApplySorting(
                filterParams.SortBy,
                filterParams.IsDescending,
                sortExpressions,
                defaultSort: s => s.StartDate);

            // Get paged results
            var (items, totalCount) = await query.ToPagedListAsync(
                filterParams.GetPage(),
                filterParams.GetPageSize());

            // Map to DTOs
            var dtos = items.Select(s => new SubscriptionDto
            {
                Id = s.Id,
                ManagerId = s.ManagerId,
                ManagerName = s.Manager.User.FullName,
                SubscriptionPlanId = s.SubscriptionPlanId,
                PlanName = s.Plan.Name,
                PlanPrice = s.Plan.PricePerMonth,
                MaxEmployees = s.Plan.MaxEmployees,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                AutoRenew = s.AutoRenew
            }).ToList();

            return PagedResponse<SubscriptionDto>.Create(
                dtos,
                totalCount,
                filterParams.GetPage(),
                filterParams.GetPageSize());
        }

        private SubscriptionDto MapToSubscriptionDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                ManagerId = subscription.ManagerId,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                PlanName = subscription.Plan!.Name,
                PlanPrice = subscription.Plan.PricePerMonth,
                MaxEmployees = subscription.Plan.MaxEmployees,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                IsActive = subscription.IsActive,
                AutoRenew = subscription.AutoRenew
            };
        }
    }
}

