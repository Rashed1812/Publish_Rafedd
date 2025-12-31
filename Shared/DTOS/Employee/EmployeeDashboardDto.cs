namespace Shared.DTOS.Employee
{
    public class EmployeeDashboardDto
    {
        public TaskStatsDto TaskStats { get; set; } = new();
        public ReportStatsDto ReportStats { get; set; } = new();
        public PerformanceDto Performance { get; set; } = new();
        public CurrentWeekInfoDto CurrentWeek { get; set; } = new();
    }

    public class TaskStatsDto
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
        public int CompletionPercentage { get; set; }
    }

    public class ReportStatsDto
    {
        public int TotalReports { get; set; }
        public int ReportsThisWeek { get; set; }
    }

    public class PerformanceDto
    {
        public double AverageScore { get; set; }
        public string PerformanceLevel { get; set; } = string.Empty; // Excellent, Good, Average, Poor
    }

    public class CurrentWeekInfoDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int WeekNumber { get; set; }
    }
}
