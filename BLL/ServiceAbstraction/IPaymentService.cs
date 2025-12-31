using Shared.DTOS.Payment;

namespace BLL.ServiceAbstraction
{
    public interface IPaymentService
    {
        // Stripe
        Task<PaymentIntentResponseDto> CreateStripePaymentIntentAsync(CreatePaymentDto dto);
        Task<bool> HandleStripeWebhookAsync(string json, string signature);
        
        // My Fatoorah
        Task<MyFatoorahInitResponseDto> InitiateMyFatoorahPaymentAsync(CreatePaymentDto dto);
        Task<bool> HandleMyFatoorahCallbackAsync(string paymentId, string invoiceId);
        
        // PayTabs (Optional - keeping for compatibility)
        Task<PayTabsInitResponseDto> InitiatePayTabsPaymentAsync(CreatePaymentDto dto);
        Task<bool> HandlePayTabsCallbackAsync(string transactionRef, string paymentResult);
        
        // General
        Task<PaymentDto> GetPaymentByTransactionIdAsync(string transactionId);
        Task<List<PaymentDto>> GetPaymentsByManagerAsync(string managerUserId);
        Task<bool> VerifyPaymentStatusAsync(string transactionId);
    }
}

