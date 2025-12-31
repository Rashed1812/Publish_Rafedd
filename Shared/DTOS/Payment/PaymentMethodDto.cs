namespace Shared.DTOS.Payment
{
    public class PaymentMethodDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!; // card, bank_transfer, other
        public CardDetailsDto? Card { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CardDetailsDto
    {
        public string Brand { get; set; } = null!; // visa, mastercard, etc.
        public string Last4 { get; set; } = null!;
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string? HolderName { get; set; }
    }

    public class CreatePaymentMethodDto
    {
        public string Type { get; set; } = "card";
        public CardInputDto? Card { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    public class CardInputDto
    {
        public string Number { get; set; } = null!;
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string Cvc { get; set; } = null!;
        public string HolderName { get; set; } = null!;
    }

    public class UpdatePaymentMethodDto
    {
        public bool? IsDefault { get; set; }
        public int? ExpMonth { get; set; }
        public int? ExpYear { get; set; }
    }
}

