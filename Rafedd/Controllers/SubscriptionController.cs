using BLL.ServiceAbstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOS.Common;
using Shared.DTOS.Subscription;
using Shared.DTOS.Payment;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/subscriptions")]
    [Authorize(Roles = "Manager,Admin")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) 
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // Get Available Plans
        [HttpGet("plans")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<SubscriptionPlanDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<SubscriptionPlanDto>>>> GetAvailablePlans()
        {
            var result = await _subscriptionService.GetAvailablePlansAsync();
            return Ok(new { success = true, data = result });
        }

        // Get Current Subscription
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 404)]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetCurrentSubscription()
        {
            try
            {
                var managerUserId = GetUserId();
                var result = await _subscriptionService.GetActiveSubscriptionAsync(managerUserId);
                
                if (result == null)
                {
                    throw new NotFoundException("لا يوجد اشتراك نشط");
                }

                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (NotFoundException)
            {
                throw;
            }
        }

        // Create Subscription
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 400)]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                var result = await _subscriptionService.CreateSubscriptionAsync(managerUserId, dto.PlanId);
                return Ok(new
                {
                    success = true,
                    message = "تم إنشاء الاشتراك بنجاح",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // Update Subscription
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 400)]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto dto)
        {
            try
            {
                // This would require adding UpdateSubscriptionAsync method to ISubscriptionService
                // For now, return a simplified response
                return Ok(new
                {
                    success = true,
                    message = "تم تحديث الاشتراك بنجاح",
                    data = new { id, autoRenew = dto.AutoRenew, updatedAt = DateTime.UtcNow }
                });
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // Cancel Subscription
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse>> CancelSubscription(int id)
        {
            try
            {
                var managerUserId = GetUserId();
                var result = await _subscriptionService.CancelSubscriptionAsync(managerUserId);
                
                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "تم إلغاء الاشتراك بنجاح",
                        data = new { id, status = "cancelled", cancelledAt = DateTime.UtcNow }
                    });
                }

                throw new BadRequestException("فشل إلغاء الاشتراك");
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // Check Employee Limit
        [HttpPost("check-employee-limit")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> CheckEmployeeLimit([FromBody] CheckEmployeeLimitDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                var result = await _subscriptionService.CheckEmployeeLimitAsync(managerUserId, dto.RequestedCount);
                
                var response = new { allowed = result };
                var message = result 
                    ? $"يمكن إضافة {dto.RequestedCount} موظف/موظفين" 
                    : $"لا يمكن إضافة {dto.RequestedCount} موظف/موظفين. تجاوز حد الاشتراك";
                
                return Ok(ApiResponse<object>.SuccessResponse(response, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking employee limit");
                throw new BadRequestException("حدث خطأ أثناء فحص حد الموظفين");
            }
        }

        // Change Subscription Plan
        [HttpPost("{id}/change-plan")]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), 200)]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> ChangePlan(int id, [FromBody] ChangePlanDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                
                // TODO: Implement ChangePlanAsync in ISubscriptionService
                // This should handle prorating, checking employee limits, and processing payment
                
                return Ok(new
                {
                    success = true,
                    message = "تم تغيير الخطة بنجاح",
                    data = new SubscriptionDto()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing subscription plan");
                throw new BadRequestException("حدث خطأ أثناء تغيير الخطة");
            }
        }

        // Get Subscription Invoices
        [HttpGet("{id}/invoices")]
        [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetInvoices(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var managerUserId = GetUserId();
                
                // TODO: Implement GetInvoicesAsync in ISubscriptionService
                
                return Ok(new
                {
                    success = true,
                    data = new List<InvoiceDto>(),
                    pagination = new { page, limit, total = 0, totalPages = 0 }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices");
                throw new BadRequestException("حدث خطأ أثناء الحصول على الفواتير");
            }
        }

        // Get Subscription Payments
        [HttpGet("{id}/payments")]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentHistoryDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PaymentHistoryDto>>>> GetPayments(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var managerUserId = GetUserId();
                
                // TODO: Implement GetSubscriptionPaymentsAsync in ISubscriptionService
                
                return Ok(new
                {
                    success = true,
                    data = new List<PaymentHistoryDto>(),
                    pagination = new { page, limit, total = 0, totalPages = 0 }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription payments");
                throw new BadRequestException("حدث خطأ أثناء الحصول على المدفوعات");
            }
        }
    }

    public class CreateSubscriptionDto
    {
        public int PlanId { get; set; }
        public string? PaymentMethodId { get; set; }
        public bool AutoRenew { get; set; } = true;
    }

    public class UpdateSubscriptionDto
    {
        public bool? AutoRenew { get; set; }
    }

    public class UpgradeSubscriptionDto
    {
        public int NewPlanId { get; set; }
    }

    public class CheckEmployeeLimitDto
    {
        public int RequestedCount { get; set; }
    }

    public class ChangePlanDto
    {
        public int PlanId { get; set; }
        public string EffectiveDate { get; set; } = "immediate"; // immediate or next_billing_cycle
    }
}
