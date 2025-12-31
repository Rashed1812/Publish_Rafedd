namespace Shared.DTOS.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;

        // Subscription Information (for Managers only)
        public bool HasActiveSubscription { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }
        public int? SubscriptionPlanId { get; set; }
        public string? SubscriptionPlanName { get; set; }
        public int? MaxEmployees { get; set; }
        public int? CurrentEmployeeCount { get; set; }
        public int? DaysRemaining { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}

