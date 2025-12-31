using BLL.ServiceAbstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.DTOS.Common;
using System.Security.Claims;

namespace Rafedd.Authorization
{
    /// <summary>
    /// Custom authorization attribute that requires an active subscription
    /// Allows login but blocks feature access until subscription is paid
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireActiveSubscriptionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Get the subscription validation service from DI
            var subscriptionService = context.HttpContext.RequestServices
                .GetService<ISubscriptionValidationService>();

            if (subscriptionService == null)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            // Get user ID from claims
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // User not authenticated
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse.ErrorResponse("يجب تسجيل الدخول للوصول إلى هذه الميزة"));
                return;
            }

            // Check if user has Manager or Admin role
            var isManager = context.HttpContext.User.IsInRole("Manager");
            var isAdmin = context.HttpContext.User.IsInRole("Admin");

            if (!isManager && !isAdmin)
            {
                context.Result = new ForbidResult();
                return;
            }

            // Admins bypass subscription check
            if (isAdmin)
            {
                return;
            }

            // Check if manager has active subscription
            var hasActiveSubscription = await subscriptionService.HasActiveSubscriptionAsync(userId);

            if (!hasActiveSubscription)
            {
                // Block access with clear message
                context.Result = new ObjectResult(
                    ApiResponse.ErrorResponse("يتطلب الوصول إلى هذه الميزة اشتراك نشط. يرجى الترقية لخطة الاشتراك للمتابعة."))
                {
                    StatusCode = StatusCodes.Status402PaymentRequired
                };
                return;
            }

            // Has active subscription - allow access
        }
    }
}
