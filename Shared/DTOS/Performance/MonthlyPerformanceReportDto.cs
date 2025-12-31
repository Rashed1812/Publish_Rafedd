namespace Shared.DTOS.Performance
{
    public class MonthlyPerformanceReportDto
    {
        public int Id { get; set; }
        public int MonthlyPlanId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthlyGoal { get; set; } = null!;
        public float AchievementPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public string Summary { get; set; } = null!;
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<WeeklyProgressDto> WeeklyProgress { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class WeeklyProgressDto
    {
        public int WeekNumber { get; set; }
        public float AchievementPercentage { get; set; }
    }
}
