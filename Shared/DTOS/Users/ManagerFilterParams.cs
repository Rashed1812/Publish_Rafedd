using Shared.DTOS.Common;

namespace Shared.DTOS.Users
{
    public class ManagerFilterParams : BaseFilterParams
    {
        // Optional filters
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasActiveSubscription { get; set; }
        public DateTime? SubscriptionEndDateFrom { get; set; }
        public DateTime? SubscriptionEndDateTo { get; set; }

        // Sortable fields: name, companyName, createdAt, subscriptionEndsAt
    }
}
