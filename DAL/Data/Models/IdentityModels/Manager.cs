using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.AIPlanning;
using DAL.Data.Models.Subscription;

namespace DAL.Data.Models.IdentityModels
{
    public class Manager
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        [Required, MaxLength(100)]
        public string CompanyName { get; set; } = null!;
        [Required,MaxLength(200)]
        public string BusinessType { get; set; }
        [MaxLength(200)]
        public string? BusinessDescription { get; set; }
        public int? SubscriptionId { get; set; }
        public Subscription.Subscription? Subscription { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }
        // CurrentEmployeeCount removed - use database count directly via EmployeeRepository
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Notification Preferences
        public bool EmailOnTaskReport { get; set; } = true;
        public bool EmailOnEmployeeJoin { get; set; } = true;
        public bool EmailOnTaskDeadline { get; set; } = true;
        public bool EmailOnWeeklyReport { get; set; } = true;
        
        // Dashboard Preferences
        [MaxLength(50)]
        public string DefaultView { get; set; } = "overview"; // overview, tasks, reports, employees
        public bool ShowWeeklyStats { get; set; } = true;
        public bool ShowMonthlyStats { get; set; } = true;
        public bool ShowEmployeePerformance { get; set; } = true;
        
        // Company Preferences
        [MaxLength(10)]
        public string WorkingHoursStart { get; set; } = "09:00";
        [MaxLength(10)]
        public string WorkingHoursEnd { get; set; } = "17:00";
        public int WeekStartDay { get; set; } = 0; // 0 = Sunday, 1 = Monday, etc.
        [MaxLength(100)]
        public string TimeZone { get; set; } = "Arab Standard Time"; // UTC+3

        // Navigation
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<AnnualTarget> AnnualTargets { get; set; } = new List<AnnualTarget>();
    }
}
