using Shared.DTOS.Subscription;

namespace BLL.ServiceAbstraction
{
    /// <summary>
    /// Service for validating subscription status and feature access
    /// </summary>
    public interface ISubscriptionValidationService
    {
        /// <summary>
        /// Check if manager has an active paid subscription
        /// </summary>
        Task<bool> HasActiveSubscriptionAsync(string managerUserId);

        /// <summary>
        /// Get detailed subscription status for a manager
        /// </summary>
        Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(string managerUserId);

        /// <summary>
        /// Check if manager can access a specific feature
        /// </summary>
        Task<bool> CanAccessFeatureAsync(string managerUserId, string featureName);
    }
}
