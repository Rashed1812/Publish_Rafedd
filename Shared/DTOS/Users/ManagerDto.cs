namespace Shared.DTOS.Users
{
    public class ManagerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? BusinessType { get; set; }
        public string? BusinessDescription { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public SubscriptionInfoDto? Subscription { get; set; }
    }

    public class SubscriptionInfoDto
    {
        public int Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenew { get; set; }
    }
}
