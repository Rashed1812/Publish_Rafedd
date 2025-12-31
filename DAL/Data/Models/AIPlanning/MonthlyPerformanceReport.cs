namespace DAL.Data.Models.AIPlanning
{
    /// <summary>
    /// Represents a monthly performance analysis report that aggregates all 4 weekly reports
    /// and compares performance against the MonthlyPlan goal
    /// </summary>
    public class MonthlyPerformanceReport
    {
        public int Id { get; set; }

        // Relationship
        public int MonthlyPlanId { get; set; }
        public MonthlyPlan MonthlyPlan { get; set; } = null!;

        // Achievement Data
        public float AchievementPercentage { get; set; }  // 0-100
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }

        // AI Analysis Results (Arabic)
        public string Summary { get; set; } = null!;               // Overall Arabic summary
        public string? Strengths { get; set; }            // JSON array of strengths
        public string? Weaknesses { get; set; }           // JSON array of weaknesses
        public string? Recommendations { get; set; }      // JSON array of recommendations

        // Weekly Breakdown
        public string? WeeklyProgressSummary { get; set; } // JSON: week-by-week achievement percentages

        // Metadata
        public DateTime GeneratedAt { get; set; }
    }
}
