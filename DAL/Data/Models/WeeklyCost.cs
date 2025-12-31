using System;
using System.ComponentModel.DataAnnotations;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models
{
    public class WeeklyCost
    {
        public int Id { get; set; }
        
        [Required]
        public string EmployeeId { get; set; } = null!;
        public Employee Employee { get; set; } = null!;
        
        [Required]
        [Range(1, 4)]
        public int WeekNumber { get; set; }
        
        [Required]
        [Range(1, 12)]
        public int Month { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = null!;
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string CostType { get; set; } = "expense"; // salary, bonus, expense, other
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // paid, pending
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}

