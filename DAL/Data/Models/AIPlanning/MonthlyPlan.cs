using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data.Models.AIPlanning
{
    public class MonthlyPlan
    {
        public int Id { get; set; }
        public int AnnualTargetId { get; set; }
        public AnnualTarget AnnualTarget { get; set; } = null!;
        public int Month { get; set; } // 1-12
        public string MonthlyGoal { get; set; } = null!;
        public int Year { get; set; }
        public ICollection<WeeklyPlan> WeeklyPlans { get; set; } = new List<WeeklyPlan>();

        // Performance Tracking
        public float? AchievementPercentage { get; set; }
        public MonthlyPerformanceReport? MonthlyPerformanceReport { get; set; }
    }
}
