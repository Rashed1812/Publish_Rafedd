namespace Shared.DTOS.Admin
{
    public class AdminSettingsDto
    {
        public PlatformSettings Platform { get; set; } = new();
        public DefaultConfigurations Defaults { get; set; } = new();
        public FeatureFlags Features { get; set; } = new();
    }

    public class PlatformSettings
    {
        public bool MaintenanceMode { get; set; } = false;
        public string? SystemAnnouncement { get; set; }
        public bool AllowNewRegistrations { get; set; } = true;
        public bool RequireEmailVerification { get; set; } = true;
    }

    public class DefaultConfigurations
    {
        public int TrialPeriodDays { get; set; } = 14;
        public int DefaultPlanId { get; set; } = 1;
        public int SessionTimeoutMinutes { get; set; } = 60;
        public int MaxLoginAttempts { get; set; } = 5;
    }

    public class FeatureFlags
    {
        public bool EnableNotifications { get; set; } = true;
        public bool EnableReports { get; set; } = true;
        public bool EnablePayments { get; set; } = true;
        public bool EnableUserActivities { get; set; } = true;
    }
}
