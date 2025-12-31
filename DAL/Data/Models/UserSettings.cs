using System;
using System.ComponentModel.DataAnnotations;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models
{
    public class UserSettings
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        
        [MaxLength(10)]
        public string Language { get; set; } = "ar"; // ar, en
        
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        
        [MaxLength(20)]
        public string Theme { get; set; } = "light"; // light, dark
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

