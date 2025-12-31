using System.Collections.Generic;

namespace Shared.DTOS.Payment
{
    public class ProcessPaymentDto
    {
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; } = null!;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class PaymentHistoryDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? Description { get; set; }
        public PaymentMethodDto? PaymentMethod { get; set; }
        public int? SubscriptionId { get; set; }
        public int? InvoiceId { get; set; }
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefundPaymentDto
    {
        public decimal? Amount { get; set; } // Optional: Partial refund. If not provided, full refund
        public string Reason { get; set; } = null!;
    }

    public class ValidatePromoDto
    {
        public string Code { get; set; } = null!;
        public int? PlanId { get; set; }
    }

    public class PromoValidationResultDto
    {
        public string Code { get; set; } = null!;
        public string Type { get; set; } = null!; // percentage, fixed_amount
        public decimal Value { get; set; }
        public string Description { get; set; } = null!;
        public bool Valid { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }
}

