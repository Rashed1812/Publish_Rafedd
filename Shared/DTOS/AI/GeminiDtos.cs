namespace Shared.DTOS.AI
{
    // Annual Plan Generation
    public class AnnualPlanGenerationDto
    {
        public int Year { get; set; }
        public string Goal { get; set; } = null!;
        public List<MonthlyGoalDto> MonthlyGoals { get; set; } = new();
    }

    public class MonthlyGoalDto
    {
        public int Month { get; set; }
        public string Description { get; set; } = null!;
        public List<WeeklyGoalDto> WeeklyGoals { get; set; } = new();
    }

    public class WeeklyGoalDto
    {
        public int WeekNumber { get; set; }
        public string Description { get; set; } = null!;
    }

    // Monthly Report Generation
    public class MonthlyReportRequestDto
    {
        public string Goal { get; set; } = null!;
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public List<EmployeePerformanceSummaryDto> EmployeeStats { get; set; } = new();
    }

    public class EmployeePerformanceSummaryDto
    {
        public string EmployeeName { get; set; } = null!;
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public double CompletionRate { get; set; }
    }

    public class MonthlyReportGenerationDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string OverallSummary { get; set; } = null!;
        public List<string> TopPerformers { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    // Employee Performance Analysis
    public class EmployeePerformanceDataDto
    {
        public string EmployeeName { get; set; } = null!;
        public int TasksCompleted { get; set; }
        public int TasksMissed { get; set; }
        public double AverageCompletionTime { get; set; }
        public string RecentActivity { get; set; } = null!;
    }
}
