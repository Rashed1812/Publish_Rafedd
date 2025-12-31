using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Manager
{
    public class ManagerSettingsDto
    {
        public NotificationPreferences Notifications { get; set; } = new();
        public DashboardPreferences Dashboard { get; set; } = new();
        public CompanyPreferences Company { get; set; } = new();
        public CompanyInfo CompanyInfo { get; set; } = new();
    }

    public class NotificationPreferences
    {
        public bool EmailOnTaskReport { get; set; } = true;
        public bool EmailOnEmployeeJoin { get; set; } = true;
        public bool EmailOnTaskDeadline { get; set; } = true;
        public bool EmailOnWeeklyReport { get; set; } = true;
    }

    public class DashboardPreferences
    {
        public string DefaultView { get; set; } = "overview"; // overview, tasks, reports, employees
        public bool ShowWeeklyStats { get; set; } = true;
        public bool ShowMonthlyStats { get; set; } = true;
        public bool ShowEmployeePerformance { get; set; } = true;
    }

    public class CompanyPreferences
    {
        public string WorkingHoursStart { get; set; } = "09:00";
        public string WorkingHoursEnd { get; set; } = "17:00";
        public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Sunday;
        public string TimeZone { get; set; } = "Arab Standard Time"; // UTC+3
    }

    public class CompanyInfo
    {
        [Required, MaxLength(100)]
        public string CompanyName { get; set; } = null!;
        [Required, MaxLength(200)]
        public string BusinessType { get; set; } = null!;
    }
}
