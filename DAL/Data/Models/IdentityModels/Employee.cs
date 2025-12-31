using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.TasksAndReports;

namespace DAL.Data.Models.IdentityModels
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? ManagerUserId { get; set; }
        public ApplicationUser? Manager { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Position { get; set; } = null!;
        
        [MaxLength(100)]
        public string? Department { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
        public ICollection<TaskReport> Reports { get; set; } = new List<TaskReport>();
    }
}
