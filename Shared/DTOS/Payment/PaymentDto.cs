namespace Shared.DTOS.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? TransactionId { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; } // Only set when payment is confirmed as Completed
    }

    public class PaymentIntentResponseDto
    {
        public string ClientSecret { get; set; } = null!;
        public string PaymentIntentId { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
    }

    public class MyFatoorahInitResponseDto
    {
        public string PaymentUrl { get; set; } = null!;
        public string InvoiceId { get; set; } = null!;
        public string InvoiceRef { get; set; } = null!;
    }

    public class PayTabsInitResponseDto
    {
        public string RedirectUrl { get; set; } = null!;
        public string TransactionRef { get; set; } = null!;
        public string PaymentUrl { get; set; } = null!;
    }
}

