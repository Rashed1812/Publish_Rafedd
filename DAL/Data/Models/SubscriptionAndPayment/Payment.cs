using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.Subscription
{
    public class Payment
    {
        public int Id { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public int? SubscriptionId { get; set; }
        public Subscription? Subscription { get; set; }
        
        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // pending, completed, failed, refunded
        
        [MaxLength(50)]
        public string Type { get; set; } = "subscription_payment"; // subscription_payment, one_time, refund

        public int? PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public string? Metadata { get; set; } // JSON string for additional data

        public DateTime? PaidAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        
        public string? TransactionId { get; set; }
        
        [MaxLength(100)]
        public string? PaymentMethodName { get; set; } // Legacy field

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
