using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data.Models.AIPlanning
{
    public class WeeklyPlan
    {
        public int Id { get; set; }

        public int MonthlyPlanId { get; set; }
        public MonthlyPlan MonthlyPlan { get; set; } = null!;

        public int WeekNumber { get; set; }

        public string WeeklyGoal { get; set; } = null!;

        public int Year { get; set; }
        public int Month { get; set; }

        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }

        public float? AchievementPercentage { get; set; }
        public DateTime? ReportGeneratedAt { get; set; }

        public PerformanceReport? PerformanceReport { get; set; }
    }
}
