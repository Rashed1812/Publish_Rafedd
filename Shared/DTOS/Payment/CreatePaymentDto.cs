using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Payment
{
    public class CreatePaymentDto
    {
        [Required]
        public int SubscriptionId { get; set; }
        [Required]
        public string UserId{ get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        [Required]
        public string PaymentMethod { get; set; } = null!; // "stripe", "myfatoorah", or "paytabs"

        public string? Description { get; set; }
    }
}

