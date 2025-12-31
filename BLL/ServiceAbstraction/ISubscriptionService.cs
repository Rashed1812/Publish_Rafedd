using Shared.DTOS.Common;
using Shared.DTOS.Subscription;

namespace BLL.ServiceAbstraction
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDto> CreateSubscriptionAsync(string managerUserId, int subscriptionPlanId);
        Task<SubscriptionDto?> GetActiveSubscriptionAsync(string managerUserId);
        Task<SubscriptionDto> UpgradeSubscriptionAsync(string managerUserId, int newPlanId);
        Task<bool> CancelSubscriptionAsync(string managerUserId);
        Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();
        Task<bool> CheckEmployeeLimitAsync(string managerUserId, int requestedCount);
        Task<PagedResponse<SubscriptionDto>> GetSubscriptionsAsync(SubscriptionFilterParams filterParams);
    }
}

