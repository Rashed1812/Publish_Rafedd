using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.Subscription;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Payment;
using Stripe;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BLL.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ApplicationDbContext context,
            IPaymentRepository paymentRepository,
            ISubscriptionRepository subscriptionRepository,
            IManagerRepository managerRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _managerRepository = managerRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Initialize Stripe
            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(stripeSecretKey))
            {
                StripeConfiguration.ApiKey = stripeSecretKey;
            }
        }

        public async Task<PaymentIntentResponseDto> CreateStripePaymentIntentAsync(CreatePaymentDto dto)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(dto.SubscriptionId);
                if (subscription == null)
                {
                    throw new InvalidOperationException("Subscription not found");
                }

                // Create Payment Intent in Stripe
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(dto.Amount * 100), // Convert to cents
                    Currency = dto.Currency.ToLower(),
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "subscription_id", dto.SubscriptionId.ToString() },
                        { "manager_id", subscription.ManagerId.ToString() }
                    },
                    Description = dto.Description ?? $"Subscription payment for {subscription.Manager.CompanyName}"
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // Create payment record in database
                var payment = new Payment
                {
                    SubscriptionId = dto.SubscriptionId,
                    Amount = dto.Amount,
                    Currency = dto.Currency,
                    Status = "Pending",
                    TransactionId = paymentIntent.Id,
                    PaymentMethodName = "stripe"
                    // PaidAt will be set when payment is confirmed as Completed
                };

                await _paymentRepository.AddAsync(payment);
                await _paymentRepository.SaveChangesAsync();

                _logger.LogInformation("Created Stripe Payment Intent: {PaymentIntentId} for Subscription: {SubscriptionId}", 
                    paymentIntent.Id, dto.SubscriptionId);

                return new PaymentIntentResponseDto
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    TransactionId = paymentIntent.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe payment intent");
                throw;
            }
        }

        public async Task<bool> HandleStripeWebhookAsync(string json, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    _logger.LogWarning("Stripe webhook secret not configured");
                    return false;
                }

                var stripeEvent = EventUtility.ParseEvent(json);
                stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

                _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        if (paymentIntent != null)
                        {
                            await HandleSuccessfulPaymentAsync(paymentIntent.Id);
                        }
                        break;

                    case "payment_intent.payment_failed":
                        var failedPayment = stripeEvent.Data.Object as PaymentIntent;
                        if (failedPayment != null)
                        {
                            await HandleFailedPaymentAsync(failedPayment.Id);
                        }
                        break;

                    case "charge.refunded":
                        var charge = stripeEvent.Data.Object as Charge;
                        if (charge != null && !string.IsNullOrEmpty(charge.PaymentIntentId))
                        {
                            await HandleRefundAsync(charge.PaymentIntentId);
                        }
                        break;
                }

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return false;
            }
        }

        public async Task<MyFatoorahInitResponseDto> InitiateMyFatoorahPaymentAsync(CreatePaymentDto dto)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(dto.SubscriptionId);
                if (subscription == null)
                {
                    throw new InvalidOperationException("Subscription not found");
                }

                var apiToken = _configuration["MyFatoorah:ApiToken"];
                var baseUrl = _configuration["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com";

                if (string.IsNullOrEmpty(apiToken))
                {
                    throw new InvalidOperationException("My Fatoorah API token not configured");
                }

                var invoiceRef = Guid.NewGuid().ToString("N")[..16];

                // Get the callback URL - ensure it's a valid public URL
                var callbackBaseUrl = _configuration["AppSettings:BaseUrl"];
                if (string.IsNullOrEmpty(callbackBaseUrl) || callbackBaseUrl.Contains("localhost"))
                {
                    throw new InvalidOperationException("AppSettings:BaseUrl must be configured with a valid public URL for MyFatoorah callbacks (not localhost)");
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");

                // Step 1: Call InitiatePayment to get available payment methods
                var initiateRequestBody = new
                {
                    InvoiceAmount = dto.Amount,
                    CurrencyIso = dto.Currency
                };

                var initiateJson = JsonSerializer.Serialize(initiateRequestBody);
                var initiateContent = new StringContent(initiateJson, Encoding.UTF8, "application/json");

                // Log the request for support debugging
                _logger.LogInformation("=== MyFatoorah InitiatePayment API Call ===");
                _logger.LogInformation("API URL: {BaseUrl}/v2/InitiatePayment", baseUrl);
                _logger.LogInformation("Request JSON: {Request}", initiateJson);

                var initiateResponse = await httpClient.PostAsync($"{baseUrl}/v2/InitiatePayment", initiateContent);
                var initiateResponseJson = await initiateResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Response Status: {StatusCode}", initiateResponse.StatusCode);
                _logger.LogInformation("Response JSON: {Response}", initiateResponseJson);

                if (!initiateResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("MyFatoorah InitiatePayment Error: {StatusCode} - {Response}", initiateResponse.StatusCode, initiateResponseJson);
                    throw new InvalidOperationException($"MyFatoorah InitiatePayment failed: {initiateResponseJson}");
                }

                // Get the first available payment method (VISA/MASTER typically has ID 2)
                using var initiateDoc = JsonDocument.Parse(initiateResponseJson);
                var initiateRoot = initiateDoc.RootElement;
                int paymentMethodId = 2; // Default to VISA/MASTER

                if (initiateRoot.TryGetProperty("Data", out var initiateData) &&
                    initiateData.TryGetProperty("PaymentMethods", out var paymentMethods) &&
                    paymentMethods.GetArrayLength() > 0)
                {
                    // Use the first payment method available
                    paymentMethodId = paymentMethods[0].GetProperty("PaymentMethodId").GetInt32();
                }

                // Step 2: Call ExecutePayment to create the invoice
                var executeRequestBody = new
                {
                    PaymentMethodId = paymentMethodId,
                    InvoiceValue = dto.Amount,
                    CallBackUrl = $"http://localhost:3000/api/v1/payment/myfatoorah/callback",
                    ErrorUrl = $"http://localhost:3000/api/v1/payment/myfatoorah/error",
                    CustomerName = subscription.Manager.User.FullName ?? "Customer",
                    CustomerEmail = subscription.Manager.User.Email,
                    Language = "en",
                    DisplayCurrencyIso = dto.Currency
                };

                var json = JsonSerializer.Serialize(executeRequestBody);

                // Log the request for support debugging
                _logger.LogInformation("=== MyFatoorah ExecutePayment API Call ===");
                _logger.LogInformation("API URL: {BaseUrl}/v2/ExecutePayment", baseUrl);
                _logger.LogInformation("Request JSON: {Request}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{baseUrl}/v2/ExecutePayment", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response JSON: {Response}", responseJson);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("MyFatoorah API Error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                    throw new InvalidOperationException($"MyFatoorah API returned {response.StatusCode}: {responseJson}");
                }

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("IsSuccess", out var isSuccess) &&
                    isSuccess.GetBoolean() &&
                    root.TryGetProperty("Data", out var data))
                {
                    var invoiceId = data.TryGetProperty("InvoiceId", out var invId) ? invId.GetInt32().ToString() : "";
                    var paymentUrl = data.TryGetProperty("PaymentURL", out var url) ? url.GetString() : "";

                    if (!string.IsNullOrEmpty(invoiceId) && !string.IsNullOrEmpty(paymentUrl))
                    {
                        // Create payment record in database
                        var payment = new Payment
                        {
                            SubscriptionId = dto.SubscriptionId,
                            Amount = dto.Amount,
                            Currency = dto.Currency,
                            Status = "Pending",
                            TransactionId = invoiceId,
                            PaymentMethodName = "myfatoorah",
                            UserId = dto.UserId
                            // PaidAt will be set when payment is confirmed as Completed
                        };

                        await _paymentRepository.AddAsync(payment);
                        await _paymentRepository.SaveChangesAsync();

                        _logger.LogInformation("Initiated My Fatoorah payment: Invoice {InvoiceId} for Subscription: {SubscriptionId}", 
                            invoiceId, dto.SubscriptionId);

                        return new MyFatoorahInitResponseDto
                        {
                            PaymentUrl = paymentUrl,
                            InvoiceId = invoiceId,
                            InvoiceRef = invoiceRef
                        };
                    }
                }

                throw new InvalidOperationException("Invalid response from My Fatoorah");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating My Fatoorah payment");
                throw;
            }
        }

        public async Task<bool> HandleMyFatoorahCallbackAsync(string paymentId, string invoiceId)
        {
            try
            {
                var payment = await _paymentRepository.GetByTransactionIdAsync(invoiceId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for invoice: {InvoiceId}", invoiceId);
                    return false;
                }

                // Verify payment with My Fatoorah
                var apiToken = _configuration["MyFatoorah:ApiToken"];
                var baseUrl = _configuration["MyFatoorah:BaseUrl"] ?? "https://apitest.myfatoorah.com";

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");

                // Try with PaymentId first
                var verifyRequestBody = new
                {
                    Key = paymentId,
                    KeyType = "PaymentId"
                };

                var json = JsonSerializer.Serialize(verifyRequestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var verifyResponse = await httpClient.PostAsync($"{baseUrl}/v2/GetPaymentStatus", content);

                if (!verifyResponse.IsSuccessStatusCode)
                {
                    // Try with InvoiceId
                    verifyRequestBody = new
                    {
                        Key = invoiceId,
                        KeyType = "InvoiceId"
                    };
                    json = JsonSerializer.Serialize(verifyRequestBody);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                    verifyResponse = await httpClient.PostAsync($"{baseUrl}/v2/GetPaymentStatus", content);
                }

                verifyResponse.EnsureSuccessStatusCode();

                var responseJson = await verifyResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("IsSuccess", out var isSuccess) && 
                    isSuccess.GetBoolean() &&
                    root.TryGetProperty("Data", out var data))
                {
                    var invoiceStatus = data.TryGetProperty("InvoiceStatus", out var status) 
                        ? status.GetString() 
                        : "";

                    if (invoiceStatus == "Paid")
                    {
                        // Payment successful
                        await HandleSuccessfulPaymentAsync(invoiceId);
                        return true;
                    }
                    else if (invoiceStatus == "Failed" || invoiceStatus == "Canceled")
                    {
                        // Payment failed
                        await HandleFailedPaymentAsync(invoiceId);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling My Fatoorah callback");
                return false;
            }
        }

        public async Task<PayTabsInitResponseDto> InitiatePayTabsPaymentAsync(CreatePaymentDto dto)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(dto.SubscriptionId);
                if (subscription == null)
                {
                    throw new InvalidOperationException("Subscription not found");
                }

                var profileId = _configuration["PayTabs:ProfileId"];
                var serverKey = _configuration["PayTabs:ServerKey"];
                var baseUrl = _configuration["PayTabs:BaseUrl"] ?? "https://secure.paytabs.com";

                if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(serverKey))
                {
                    throw new InvalidOperationException("PayTabs credentials not configured");
                }

                var transactionRef = Guid.NewGuid().ToString("N")[..16];

                // Prepare PayTabs payment request
                var requestBody = new
                {
                    profile_id = profileId,
                    tran_type = "sale",
                    tran_class = "ecom",
                    cart_id = $"SUB-{dto.SubscriptionId}",
                    cart_currency = dto.Currency,
                    cart_amount = dto.Amount,
                    cart_description = dto.Description ?? $"Subscription payment for {subscription.Manager.CompanyName}",
                    paypage_lang = "ar",
                    customer_details = new
                    {
                        name = subscription.Manager.User.FullName,
                        email = subscription.Manager.User.Email,
                        phone = subscription.Manager.User.PhoneNumber ?? "",
                        street1 = "",
                        city = "",
                        state = "",
                        country = "SA",
                        zip = ""
                    },
                    shipping_details = new
                    {
                        name = subscription.Manager.User.FullName,
                        email = subscription.Manager.User.Email,
                        phone = subscription.Manager.User.PhoneNumber ?? "",
                        street1 = "",
                        city = "",
                        state = "",
                        country = "SA",
                        zip = ""
                    },
                    callback = $"{_configuration["AppSettings:BaseUrl"]}/api/payment/paytabs/callback",
                    return_url = $"{_configuration["AppSettings:BaseUrl"]}/payment/success?ref={transactionRef}"
                };

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", serverKey);

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{baseUrl}/payment/request", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("tran_ref", out var tranRef) &&
                    root.TryGetProperty("redirect_url", out var redirectUrl))
                {
                    // Create payment record in database
                    var payment = new Payment
                    {
                        SubscriptionId = dto.SubscriptionId,
                        Amount = dto.Amount,
                        Currency = dto.Currency,
                        Status = "Pending",
                        TransactionId = tranRef.GetString(),
                        PaymentMethodName = "paytabs"
                        // PaidAt will be set when payment is confirmed as Completed
                    };

                    await _paymentRepository.AddAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    _logger.LogInformation("Initiated PayTabs payment: {TransactionRef} for Subscription: {SubscriptionId}", 
                        tranRef.GetString(), dto.SubscriptionId);

                    return new PayTabsInitResponseDto
                    {
                        RedirectUrl = redirectUrl.GetString()!,
                        TransactionRef = tranRef.GetString()!,
                        PaymentUrl = redirectUrl.GetString()!
                    };
                }

                throw new InvalidOperationException("Invalid response from PayTabs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayTabs payment");
                throw;
            }
        }

        public async Task<bool> HandlePayTabsCallbackAsync(string transactionRef, string paymentResult)
        {
            try
            {
                var payment = await _paymentRepository.GetByTransactionIdAsync(transactionRef);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for transaction: {TransactionRef}", transactionRef);
                    return false;
                }

                // Verify payment with PayTabs
                var serverKey = _configuration["PayTabs:ServerKey"];
                var baseUrl = _configuration["PayTabs:BaseUrl"] ?? "https://secure.paytabs.com";

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", serverKey);

                var verifyResponse = await httpClient.GetAsync($"{baseUrl}/payment/query/{transactionRef}");
                verifyResponse.EnsureSuccessStatusCode();

                var responseJson = await verifyResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("payment_result", out var paymentResultElement))
                {
                    var result = paymentResultElement.GetProperty("response_status").GetString();
                    
                    if (result == "A")
                    {
                        // Payment successful
                        await HandleSuccessfulPaymentAsync(transactionRef);
                        return true;
                    }
                    else
                    {
                        // Payment failed
                        await HandleFailedPaymentAsync(transactionRef);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayTabs callback");
                return false;
            }
        }

        public async Task<PaymentDto> GetPaymentByTransactionIdAsync(string transactionId)
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
            if (payment == null)
            {
                throw new InvalidOperationException("Payment not found");
            }

            return MapToPaymentDto(payment);
        }

        public async Task<List<PaymentDto>> GetPaymentsByManagerAsync(string managerUserId)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);
            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            var subscription = await _subscriptionRepository.GetByManagerIdAsync(manager.Id);
            if (subscription == null)
            {
                return new List<PaymentDto>();
            }

            var payments = await _paymentRepository.GetAllWithDetailsAsync();
            var managerPayments = payments
                .Where(p => p.Subscription.ManagerId == manager.Id)
                .OrderByDescending(p => p.PaidAt ?? DateTime.MinValue) // Order by PaidAt if available, otherwise by minimum date (pending payments last)
                .ThenByDescending(p => p.Id) // Then by Id for consistent ordering
                .ToList();

            return managerPayments.Select(MapToPaymentDto).ToList();
        }

        public async Task<bool> VerifyPaymentStatusAsync(string transactionId)
        {
            try
            {
                var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
                if (payment == null)
                {
                    return false;
                }

                if (payment.Status == "Completed")
                {
                    return true;
                }

                // Verify with payment gateway
                if (payment.PaymentMethodName == "stripe")
                {
                    var service = new PaymentIntentService();
                    var paymentIntent = await service.GetAsync(transactionId);
                    
                    if (paymentIntent.Status == "succeeded")
                    {
                        await HandleSuccessfulPaymentAsync(transactionId);
                        return true;
                    }
                }
                else if (payment.PaymentMethodName == "myfatoorah")
                {
                    return await HandleMyFatoorahCallbackAsync("", transactionId);
                }
                else if (payment.PaymentMethodName == "paytabs")
                {
                    return await HandlePayTabsCallbackAsync(transactionId, "");
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment status");
                return false;
            }
        }

        private async Task HandleSuccessfulPaymentAsync(string transactionId)
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
            if (payment == null || payment.Status == "Completed")
            {
                return;
            }

            payment.Status = "Completed";
            payment.PaidAt = DateTime.UtcNow; // Set PaidAt only when payment is confirmed as Completed
            _paymentRepository.Update(payment);

            // Activate or renew subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(payment.SubscriptionId ?? 0);
            if (subscription != null)
            {
                // Validate payment amount matches plan price
                var expectedAmount = subscription.Plan!.PricePerMonth;
                if (Math.Abs(payment.Amount - expectedAmount) > 0.01m) // Allow 1 cent tolerance
                {
                    _logger.LogWarning("Payment amount {Amount} does not match plan price {Expected} for subscription {SubscriptionId}",
                        payment.Amount, expectedAmount, subscription.Id);
                    // Still process but log warning for manual review
                }
                _logger.LogInformation("Payment found: {@Payment}", payment);

                subscription.IsActive = true;

                // Check if subscription has expired
                if (subscription.EndDate < DateTime.UtcNow)
                {
                    // Expired subscription - start fresh from now
                    subscription.StartDate = DateTime.UtcNow;
                    subscription.EndDate = DateTime.UtcNow.AddMonths(1);
                    _logger.LogInformation("Renewed expired subscription {SubscriptionId} from {StartDate} to {EndDate}",
                        subscription.Id, subscription.StartDate, subscription.EndDate);
                }
                else
                {
                    // Active subscription - extend from current end date
                    subscription.EndDate = subscription.EndDate.AddMonths(1);
                    _logger.LogInformation("Extended active subscription {SubscriptionId} to {EndDate}",
                        subscription.Id, subscription.EndDate);
                }

                _subscriptionRepository.Update(subscription);

                // Update manager subscription end date
                var manager = await _managerRepository.GetByIdAsync(subscription.ManagerId);
                if (manager != null)
                {
                    _logger.LogInformation("Processed Stripe payment: , Status");
                    manager.SubscriptionEndsAt = subscription.EndDate;
                    _managerRepository.Update(manager);
                }

            }

            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment successful: {TransactionId}", transactionId);
        }

        private async Task HandleFailedPaymentAsync(string transactionId)
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
            if (payment == null || payment.Status == "Failed")
            {
                return;
            }

            payment.Status = "Failed";
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment failed: {TransactionId}", transactionId);
        }

        private async Task HandleRefundAsync(string paymentIntentId)
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(paymentIntentId);
            if (payment == null)
            {
                return;
            }

            payment.Status = "Refunded";
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment refunded: {TransactionId}", paymentIntentId);
        }

        private PaymentDto MapToPaymentDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                //Subscribtion Id Nullabe
                SubscriptionId = payment.SubscriptionId ?? 0,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                PaymentMethod = payment.PaymentMethodName,
                PaidAt = payment.PaidAt
            };
        }
    }
}

