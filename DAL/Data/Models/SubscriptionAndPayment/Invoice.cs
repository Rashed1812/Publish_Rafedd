using System;
using System.ComponentModel.DataAnnotations;

namespace DAL.Data.Models.Subscription
{
    public class Invoice
    {
        public int Id { get; set; }
        
        [Required]
        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = null!;
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // draft, pending, paid, overdue, cancelled
        
        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        
        public int? PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }
        
        public string? DownloadUrl { get; set; }
        
        public DateTime? PaidAt { get; set; }
        public DateTime DueDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

