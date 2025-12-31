using Shared.DTOS.Common;
using Shared.Exceptions;
using System.Net;
using System.Text.Json;

namespace Rafedd.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;

            ApiResponse<object> errorResponse;

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = ApiResponse<object>.NotFoundResponse(notFoundEx.Message);
                    break;

                case BadRequestException badRequestEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = ApiResponse<object>.ErrorResponse(
                        badRequestEx.Message,
                        badRequestEx.StatusCode,
                        badRequestEx.Errors.Any() ? badRequestEx.Errors : null);
                    break;

                case UnauthorizedException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse = ApiResponse<object>.UnauthorizedResponse(unauthorizedEx.Message);
                    break;

                case ForbiddenException forbiddenEx:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse = ApiResponse<object>.ErrorResponse(forbiddenEx.Message, 403);
                    break;

                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = ApiResponse<object>.ErrorResponse(
                        validationEx.Message,
                        400,
                        validationEx.Errors.Any() ? validationEx.Errors : null);
                    break;

                case BusinessLogicException businessEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = ApiResponse<object>.ErrorResponse(businessEx.Message, 400);
                    break;

                case InvalidOperationException invalidOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = ApiResponse<object>.ErrorResponse(invalidOpEx.Message, 400);
                    break;

                case UnauthorizedAccessException unauthorizedAccessEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse = ApiResponse<object>.UnauthorizedResponse(unauthorizedAccessEx.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = ApiResponse<object>.ErrorResponse(
                        "حدث خطأ غير متوقع في الخادم. يرجى المحاولة مرة أخرى لاحقاً.",
                        500);
                    break;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, options);
            await response.WriteAsync(jsonResponse);
        }
    }

    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}

