using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.NotificationsLogs
{
    public class Notification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string Title { get; set; } = null!;
        
        [Required]
        public string Message { get; set; } = null!;

        public string Type { get; set; } = "system"; // task, report, suggestion, system, reminder
        
        [MaxLength(20)]
        public string Priority { get; set; } = "medium"; // low, medium, high

        public bool IsRead { get; set; } = false;
        
        public string? Link { get; set; }
        public string? RelatedId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
