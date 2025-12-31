using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.NotificationsLogs
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
