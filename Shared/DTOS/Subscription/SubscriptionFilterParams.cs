using Shared.DTOS.Common;

namespace Shared.DTOS.Subscription
{
    public class SubscriptionFilterParams : BaseFilterParams
    {
        // Optional filters
        public string? Search { get; set; } // Search by manager/company name or email
        public int? PlanId { get; set; } // Filter by plan type
        public bool? IsActive { get; set; } // Filter by active/inactive status
        public DateTime? StartDateFrom { get; set; } // Start date range - from
        public DateTime? StartDateTo { get; set; } // Start date range - to
        public DateTime? EndDateFrom { get; set; } // End date range - from
        public DateTime? EndDateTo { get; set; } // End date range - to
        public bool? AutoRenew { get; set; } // Filter by auto-renew status

        // Sortable fields: planName, startDate, endDate, managerName, isActive
    }
}
