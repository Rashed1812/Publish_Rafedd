namespace Shared.DTOS.Subscription
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Status { get; set; } = null!;
        public BillingPeriodDto BillingPeriod { get; set; } = null!;
        public PaymentMethodInfoDto? PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DownloadUrl { get; set; }
    }

    public class BillingPeriodDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class PaymentMethodInfoDto
    {
        public string Type { get; set; } = null!;
        public string? Last4 { get; set; }
        public string? Brand { get; set; }
    }
}

