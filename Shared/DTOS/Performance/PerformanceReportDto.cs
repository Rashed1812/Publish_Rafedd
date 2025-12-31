namespace Shared.DTOS.Performance
{
    public class PerformanceReportDto
    {
        public int Id { get; set; }
        public int WeeklyPlanId { get; set; }
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public float AchievementPercentage { get; set; }
        public string Summary { get; set; } = null!;
        public List<string>? Strengths { get; set; }
        public List<string>? Weaknesses { get; set; }
        public List<string>? Recommendations { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}

