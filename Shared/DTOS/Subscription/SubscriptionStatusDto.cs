namespace Shared.DTOS.Subscription
{
    public class SubscriptionStatusDto
    {
        public bool HasActiveSubscription { get; set; }
        public bool IsExpired { get; set; }
        public int? SubscriptionPlanId { get; set; }
        public string? SubscriptionPlanName { get; set; }
        public decimal? PricePerMonth { get; set; }
        public int? MaxEmployees { get; set; }
        public int CurrentEmployeeCount { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }
        public int? DaysRemaining { get; set; }
        public bool CanAddEmployees { get; set; }
        public bool CanCreateTasks { get; set; }
        public bool CanAccessAIFeatures { get; set; }
    }
}
