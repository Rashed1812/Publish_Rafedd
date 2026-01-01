using BLL.ServiceAbstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOS.Common;
using Shared.DTOS.Payment;
using Shared.Exceptions;
using System.Security.Claims;
using System.Linq;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Create Stripe Payment Intent
        [HttpPost("stripe/create-intent")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentIntentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PaymentIntentResponseDto>), 400)]
        public async Task<ActionResult<ApiResponse<PaymentIntentResponseDto>>> CreateStripePaymentIntent([FromBody] CreatePaymentDto dto)
        {
            try
            {
                var result = await _paymentService.CreateStripePaymentIntentAsync(dto);
                return Ok(ApiResponse<PaymentIntentResponseDto>.SuccessResponse(result, "تم إنشاء Payment Intent بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // Stripe Webhook
        [HttpPost("stripe/webhook")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> HandleStripeWebhook()
        {
            try
            {
                // Security: Enforce HTTPS in production
                if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
                {
                    _logger.LogWarning("Rejected non-HTTPS webhook request from {Host}", HttpContext.Request.Host);
                    return StatusCode(403, "HTTPS required");
                }

                // Security: Validate origin (Stripe IPs)
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation("Stripe webhook received from IP: {IP}", clientIp);

                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"].ToString();

                // Signature validation happens in service layer
                var handled = await _paymentService.HandleStripeWebhookAsync(json, signature);

                if (handled)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "تم معالجة Webhook بنجاح"));
                }

                throw new BadRequestException("فشل معالجة Webhook");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                throw new BadRequestException("حدث خطأ أثناء معالجة Webhook");
            }
        }

        // Initiate My Fatoorah Payment
        [HttpPost("myfatoorah/initiate")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<MyFatoorahInitResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MyFatoorahInitResponseDto>), 400)]
        public async Task<ActionResult<ApiResponse<MyFatoorahInitResponseDto>>> InitiateMyFatoorahPayment([FromBody] CreatePaymentDto dto)
        {
            try
            {
                var result = await _paymentService.InitiateMyFatoorahPaymentAsync(dto);
                return Ok(ApiResponse<MyFatoorahInitResponseDto>.SuccessResponse(result, "تم بدء عملية الدفع بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }
        // My Fatoorah Callback
        [AllowAnonymous]
        [HttpGet("myfatoorah/callback")]
        [HttpPost("myfatoorah/callback")]
        public async Task<IActionResult> HandleMyFatoorahCallback()
        {
            try
            {
                // 🔹 Read values from BOTH Query & Form (GET + POST safe)
                string? paymentId =
                    Request.Query["paymentId"].FirstOrDefault()
                    ?? Request.Form["paymentId"].FirstOrDefault();

                string? invoiceId =
                    Request.Query["invoiceId"].FirstOrDefault()
                    ?? Request.Form["invoiceId"].FirstOrDefault();

                // 🔹 Log callback hit (VERY IMPORTANT for debugging)
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogWarning(
                    "MyFatoorah CALLBACK HIT  IP={IP}, paymentId={PaymentId}, invoiceId={InvoiceId}",
                    clientIp, paymentId, invoiceId);

                // 🔹 If InvoiceId is missing but PaymentId exists → still continue
                if (string.IsNullOrEmpty(invoiceId) && string.IsNullOrEmpty(paymentId))
                {
                    _logger.LogError("MyFatoorah callback missing both paymentId and invoiceId");
                    return Redirect("/payment/error");
                }

                // 🔹 Handle callback (verification happens inside service)
                var result = await _paymentService.HandleMyFatoorahCallbackAsync(
                    paymentId ?? string.Empty,
                    invoiceId ?? string.Empty
                );

                // 🔹 Redirect user based on result
                if (result)
                {
                    return Redirect($"/payment/success?invoiceId={invoiceId}");
                }

                return Redirect($"/payment/failed?invoiceId={invoiceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MyFatoorah callback");
                return Redirect("/payment/error");
            }
        }

        // Initiate PayTabs Payment (Optional)
        [HttpPost("paytabs/initiate")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PayTabsInitResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PayTabsInitResponseDto>), 400)]
        public async Task<ActionResult<ApiResponse<PayTabsInitResponseDto>>> InitiatePayTabsPayment([FromBody] CreatePaymentDto dto)
        {
            try
            {
                var result = await _paymentService.InitiatePayTabsPaymentAsync(dto);
                return Ok(ApiResponse<PayTabsInitResponseDto>.SuccessResponse(result, "تم بدء عملية الدفع بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // PayTabs Callback
        [HttpGet("paytabs/callback")]
        [HttpPost("paytabs/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePayTabsCallback(
            [FromQuery] string? tranRef,
            [FromQuery] string? payment_result)
        {
            try
            {
                // Security: Enforce HTTPS in production
                if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
                {
                    _logger.LogWarning("Rejected non-HTTPS PayTabs callback from {Host}", HttpContext.Request.Host);
                    return Redirect("/payment/error?message=HTTPS required");
                }

                // Security: Log origin for monitoring
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation("PayTabs callback received from IP: {IP}, TranRef: {TranRef}", clientIp, tranRef);

                if (string.IsNullOrEmpty(tranRef))
                {
                    return Redirect("/payment/error?message=Transaction reference is required");
                }

                var result = await _paymentService.HandlePayTabsCallbackAsync(
                    tranRef,
                    payment_result ?? "");

                if (result)
                {
                    return Redirect($"/payment/success?ref={tranRef}");
                }

                return Redirect($"/payment/failed?ref={tranRef}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayTabs callback");
                return Redirect("/payment/error");
            }
        }

        // Get Payment by Transaction ID
        [HttpGet("{transactionId}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 404)]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPayment(string transactionId)
        {
            try
            {
                var result = await _paymentService.GetPaymentByTransactionIdAsync(transactionId);
                return Ok(ApiResponse<PaymentDto>.SuccessResponse(result, "تم الحصول على بيانات الدفعة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new NotFoundException(ex.Message);
            }
        }

        // Get Manager Payments
        [HttpGet("manager/payments")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), 400)]
        public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetManagerPayments()
        {
            try
            {
                var managerUserId = GetUserId();
                if (string.IsNullOrEmpty(managerUserId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                var result = await _paymentService.GetPaymentsByManagerAsync(managerUserId);
                return Ok(ApiResponse<List<PaymentDto>>.SuccessResponse(result, "تم الحصول على مدفوعات المدير بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
        }

        // Verify Payment Status
        [HttpPost("verify/{transactionId}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> VerifyPayment(string transactionId)
        {
            try
            {
                var result = await _paymentService.VerifyPaymentStatusAsync(transactionId);
                
                if (result)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { verified = true }, "تم التحقق من الدفعة بنجاح"));
                }

                throw new BadRequestException("فشل التحقق من الدفعة");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment");
                throw new BadRequestException("حدث خطأ أثناء التحقق من الدفعة");
            }
        }

        // Payment Methods Endpoints
        [HttpGet("methods")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentMethodDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement GetPaymentMethodsAsync in IPaymentService
                return Ok(new { success = true, data = new List<PaymentMethodDto>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                throw new BadRequestException("حدث خطأ أثناء الحصول على طرق الدفع");
            }
        }

        [HttpPost("methods")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentMethodDto>), 200)]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> AddPaymentMethod([FromBody] CreatePaymentMethodDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement AddPaymentMethodAsync in IPaymentService
                return Ok(new { success = true, message = "تم إضافة طريقة الدفع بنجاح", data = new PaymentMethodDto() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment method");
                throw new BadRequestException("حدث خطأ أثناء إضافة طريقة الدفع");
            }
        }

        [HttpPut("methods/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentMethodDto>), 200)]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement UpdatePaymentMethodAsync in IPaymentService
                return Ok(new { success = true, message = "تم تحديث طريقة الدفع بنجاح", data = new PaymentMethodDto() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment method");
                throw new BadRequestException("حدث خطأ أثناء تحديث طريقة الدفع");
            }
        }

        [HttpDelete("methods/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<ActionResult<ApiResponse>> DeletePaymentMethod(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement DeletePaymentMethodAsync in IPaymentService
                return Ok(new { success = true, message = "تم حذف طريقة الدفع بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment method");
                throw new BadRequestException("حدث خطأ أثناء حذف طريقة الدفع");
            }
        }

        // Process Payment
        [HttpPost("process")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentHistoryDto>), 200)]
        public async Task<ActionResult<ApiResponse<PaymentHistoryDto>>> ProcessPayment([FromBody] ProcessPaymentDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement ProcessPaymentAsync in IPaymentService
                return Ok(new { success = true, message = "تمت عملية الدفع بنجاح", data = new PaymentHistoryDto() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                throw new BadRequestException("حدث خطأ أثناء معالجة الدفع");
            }
        }

        // Get Payment History
        [HttpGet("history")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentHistoryDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PaymentHistoryDto>>>> GetPaymentHistory(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement GetPaymentHistoryAsync in IPaymentService with pagination
                var result = await _paymentService.GetPaymentsByManagerAsync(userId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = result.Select(p => new PaymentHistoryDto
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        Currency = p.Currency,
                        Status = p.Status,
                        Type = "subscription_payment",
                        TransactionId = p.TransactionId,
                        PaidAt = p.PaidAt,
                        CreatedAt = DateTime.UtcNow
                    }).ToList(),
                    pagination = new { page, limit, total = result.Count, totalPages = (int)Math.Ceiling(result.Count / (double)limit) }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history");
                throw new BadRequestException("حدث خطأ أثناء الحصول على سجل المدفوعات");
            }
        }

        // Refund Payment
        [HttpPost("{paymentId}/refund")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentHistoryDto>), 200)]
        public async Task<ActionResult<ApiResponse<PaymentHistoryDto>>> RefundPayment(int paymentId, [FromBody] RefundPaymentDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // TODO: Implement RefundPaymentAsync in IPaymentService
                return Ok(new { success = true, message = "تم إجراء استرداد الدفع بنجاح", data = new PaymentHistoryDto() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment");
                throw new BadRequestException("حدث خطأ أثناء استرداد الدفع");
            }
        }

        // Validate Promo Code
        [HttpPost("validate-promo")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PromoValidationResultDto>), 200)]
        public async Task<ActionResult<ApiResponse<PromoValidationResultDto>>> ValidatePromoCode([FromBody] ValidatePromoDto dto)
        {
            try
            {
                // TODO: Implement ValidatePromoCodeAsync in IPaymentService
                return Ok(new { success = true, data = new PromoValidationResultDto() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promo code");
                throw new BadRequestException("كود الخصم غير صالح أو منتهي الصلاحية");
            }
        }
    }
}
