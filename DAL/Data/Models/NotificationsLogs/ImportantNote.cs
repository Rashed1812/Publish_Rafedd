using System;
using System.ComponentModel.DataAnnotations;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.NotificationsLogs
{
    public class ImportantNote
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeId { get; set; } = null!;
        public Employee Employee { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public int? WeekNumber { get; set; } // 1-5
        public int? Month { get; set; } // 1-12
        public int? Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
