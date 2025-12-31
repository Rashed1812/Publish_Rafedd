using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.NotificationsLogs;
using DAL.Data.Models.TasksAndReports;
using Microsoft.AspNetCore.Identity;

namespace DAL.Data.Models.IdentityModels
{
    public class ApplicationUser : IdentityUser    
    {

        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Admin? Admin { get; set; }
        public Manager? Manager { get; set; }
        public Employee? Employee { get; set; }

        public ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<TaskItem> TasksCreated { get; set; } = new List<TaskItem>();
    }
}
