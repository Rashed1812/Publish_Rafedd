using System;
using System.ComponentModel.DataAnnotations;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.Subscription
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "card"; // card, bank_transfer, other
        
        // Card details (stored securely, only last4 digits)
        [MaxLength(50)]
        public string? Brand { get; set; } // visa, mastercard, etc.
        
        [MaxLength(4)]
        public string? Last4 { get; set; }
        
        public int? ExpMonth { get; set; }
        public int? ExpYear { get; set; }
        
        [MaxLength(200)]
        public string? HolderName { get; set; }
        
        public bool IsDefault { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

