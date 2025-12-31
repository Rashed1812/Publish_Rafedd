namespace Shared.DTOS.AnnualTarget
{
    public class AnnualTargetResponseDto
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public string TargetDescription { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<MonthlyPlanDto> MonthlyPlans { get; set; } = new();
    }

    public class MonthlyPlanDto
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public string MonthlyGoal { get; set; } = null!;
        public List<WeeklyPlanDto> WeeklyPlans { get; set; } = new();
    }

    public class WeeklyPlanDto
    {
        public int Id { get; set; }
        public int WeekNumber { get; set; }
        public string WeeklyGoal { get; set; } = null!;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public float? AchievementPercentage { get; set; }
    }
}

