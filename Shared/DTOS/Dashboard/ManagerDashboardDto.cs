namespace Shared.DTOS.Dashboard
{
    public class ManagerDashboardDto
    {
        public int CurrentYear { get; set; }
        public int CurrentMonth { get; set; }
        public int CurrentWeek { get; set; }
        public WeekInfoDto? CurrentWeekInfo { get; set; }
        public List<WeekInfoDto> MonthWeeks { get; set; } = new();
        public int TotalEmployees { get; set; }
        public int ActiveSubscriptions { get; set; }
        public string? CompanyName { get; set; }
        public AnnualTargetSummaryDto? CurrentAnnualTarget { get; set; }
    }

    public class WeekInfoDto
    {
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public float? AchievementPercentage { get; set; }
        public bool IsCurrentWeek { get; set; }
        public int TasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int ReportsCount { get; set; }
    }

    public class AnnualTargetSummaryDto
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public string TargetDescription { get; set; } = null!;
    }
}

