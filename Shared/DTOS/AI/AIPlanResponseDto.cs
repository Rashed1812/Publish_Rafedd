namespace Shared.DTOS.AI
{
    public class AIPlanResponseDto
    {
        public List<MonthlyPlanData> MonthlyPlans { get; set; } = new();
    }

    public class MonthlyPlanData
    {
        public int Month { get; set; }
        public string MonthlyGoal { get; set; } = null!;
        public List<WeeklyPlanData> WeeklyPlans { get; set; } = new();
    }

    public class WeeklyPlanData
    {
        public int WeekNumber { get; set; }
        public string WeeklyGoal { get; set; } = null!;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
    }

    public class AIPerformanceAnalysisDto
    {
        public float AchievementPercentage { get; set; }
        public string Summary { get; set; } = null!;
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}

