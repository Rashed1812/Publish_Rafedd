namespace Shared.DTOS.Subscription
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = null!;
        public int SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public decimal PlanPrice { get; set; }
        public int MaxEmployees { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool AutoRenew { get; set; }
    }

    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal PricePerMonth { get; set; }
        public int MaxEmployees { get; set; }
        public string? Description { get; set; }
    }
}

