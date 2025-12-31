using System;
using System.ComponentModel.DataAnnotations;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models
{
    public class Suggestion
    {
        public int Id { get; set; }
        
        [Required]
        public string EmployeeId { get; set; } = null!;
        public Employee Employee { get; set; } = null!;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;
        
        [Required]
        [MaxLength(2000)]
        public string Details { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // pending, reviewed, approved, rejected
        
        public string? Attachments { get; set; } // JSON array of URLs
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }
}

