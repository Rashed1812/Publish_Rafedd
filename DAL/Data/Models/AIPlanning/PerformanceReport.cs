using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data.Models.AIPlanning
{
    public class PerformanceReport
    {
        public int Id { get; set; }

        public int WeeklyPlanId { get; set; }
        public WeeklyPlan WeeklyPlan { get; set; } = null!;
        public float AchievementPercentage { get; set; }
        public string Summary { get; set; } = null!;
        public string? Strengths { get; set; } // JSON array
        public string? Weaknesses { get; set; } // JSON array
        public string? Recommendations { get; set; } // JSON array
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
