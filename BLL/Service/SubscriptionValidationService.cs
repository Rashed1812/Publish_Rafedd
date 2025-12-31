using BLL.ServiceAbstraction;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Subscription;

namespace BLL.Service
{
    public class SubscriptionValidationService : ISubscriptionValidationService
    {
        private readonly IManagerRepository _managerRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<SubscriptionValidationService> _logger;

        // Feature names constants
        private const string FEATURE_ADD_EMPLOYEES = "AddEmployees";
        private const string FEATURE_CREATE_TASKS = "CreateTasks";
        private const string FEATURE_AI_ANALYSIS = "AIAnalysis";
        private const string FEATURE_PERFORMANCE_REPORTS = "PerformanceReports";

        public SubscriptionValidationService(
            IManagerRepository managerRepository,
            ISubscriptionRepository subscriptionRepository,
            IEmployeeRepository employeeRepository,
            ILogger<SubscriptionValidationService> logger)
        {
            _managerRepository = managerRepository;
            _subscriptionRepository = subscriptionRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<bool> HasActiveSubscriptionAsync(string managerUserId)
        {
            try
            {
                var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
                if (manager == null)
                {
                    _logger.LogWarning("Manager not found: {ManagerUserId}", managerUserId);
                    return false;
                }

                var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);

                if (subscription == null)
                {
                    _logger.LogWarning("No subscription found for manager: {ManagerUserId}", managerUserId);
                    return false;
                }

                // Check if subscription is active and not expired
                bool isActive = subscription.IsActive &&
                               subscription.EndDate > DateTime.UtcNow;

                return isActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription status for manager: {ManagerUserId}", managerUserId);
                return false;
            }
        }

        public async Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(string managerUserId)
        {
            try
            {
                var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
                if (manager == null)
                {
                    return new SubscriptionStatusDto
                    {
                        HasActiveSubscription = false,
                        IsExpired = true,
                        CurrentEmployeeCount = 0,
                        CanAddEmployees = false,
                        CanCreateTasks = false,
                        CanAccessAIFeatures = false
                    };
                }

                var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
                var currentEmployeeCount = await _employeeRepository.GetEmployeeCountByManagerAsync(managerUserId);

                if (subscription == null)
                {
                    return new SubscriptionStatusDto
                    {
                        HasActiveSubscription = false,
                        IsExpired = true,
                        CurrentEmployeeCount = currentEmployeeCount,
                        CanAddEmployees = false,
                        CanCreateTasks = false,
                        CanAccessAIFeatures = false
                    };
                }

                var isActive = subscription.IsActive && subscription.EndDate > DateTime.UtcNow;
                var isExpired = subscription.EndDate <= DateTime.UtcNow;
                var daysRemaining = isActive ? (int)(subscription.EndDate - DateTime.UtcNow).TotalDays : 0;

                return new SubscriptionStatusDto
                {
                    HasActiveSubscription = isActive,
                    IsExpired = isExpired,
                    SubscriptionPlanId = subscription.SubscriptionPlanId,
                    SubscriptionPlanName = subscription.Plan?.Name,
                    PricePerMonth = subscription.Plan?.PricePerMonth,
                    MaxEmployees = subscription.Plan?.MaxEmployees,
                    CurrentEmployeeCount = currentEmployeeCount,
                    SubscriptionEndsAt = subscription.EndDate,
                    DaysRemaining = daysRemaining,
                    CanAddEmployees = isActive,
                    CanCreateTasks = isActive,
                    CanAccessAIFeatures = isActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for manager: {ManagerUserId}", managerUserId);
                return new SubscriptionStatusDto
                {
                    HasActiveSubscription = false,
                    IsExpired = true,
                    CurrentEmployeeCount = 0,
                    CanAddEmployees = false,
                    CanCreateTasks = false,
                    CanAccessAIFeatures = false
                };
            }
        }

        public async Task<bool> CanAccessFeatureAsync(string managerUserId, string featureName)
        {
            // For now, all features require active subscription
            // This can be extended to have feature-specific logic
            var hasActiveSubscription = await HasActiveSubscriptionAsync(managerUserId);

            if (!hasActiveSubscription)
            {
                _logger.LogWarning("Manager {ManagerUserId} attempted to access feature {FeatureName} without active subscription",
                    managerUserId, featureName);
                return false;
            }

            // Future: Add feature-specific validation
            // For example, certain features might only be available on higher-tier plans

            return true;
        }
    }
}
