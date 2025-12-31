using BLL.ServiceAbstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafedd.Authorization;
using Shared.DTOS.Auth;
using Shared.DTOS.Common;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register/manager")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterManager([FromBody] RegisterManagerDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterManagerAsync(registerDto);
                var response = ApiResponse<AuthResponseDto>.SuccessResponse(result, "تم تسجيل المدير بنجاح وتم إنشاء الاشتراك");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPost("register/employee")]
        [Authorize(Roles = "Manager")]
        [RequireActiveSubscription]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 403)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterEmployee([FromBody] RegisterEmployeeDto registerDto)
        {
            try
            {
                // Get manager userId from claims
                var managerUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(managerUserId))
                {
                    throw new UnauthorizedException("غير مصرح لك بتنفيذ هذه العملية");
                }

                var result = await _authService.RegisterEmployeeAsync(registerDto, managerUserId);
                var response = ApiResponse<AuthResponseDto>.SuccessResponse(result, "تم تسجيل الموظف بنجاح");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPost("register/admin")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 403)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterAdmin([FromBody] RegisterAdminDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAdminAsync(registerDto);
                var response = ApiResponse<AuthResponseDto>.SuccessResponse(result, "تم تسجيل المسؤول بنجاح");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // Legacy endpoint - kept for backward compatibility
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                var response = ApiResponse<AuthResponseDto>.SuccessResponse(result, "تم التسجيل بنجاح");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 401)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                // Format response according to API Docs
                var response = new
                {
                    success = true,
                    token = result.Token,
                    refreshToken = result.RefreshToken,
                    user = new
                    {
                        id = result.User.Id,
                        name = result.User.FullName,
                        email = result.User.Email,
                        phone = "",
                        role = result.User.Role.ToLower(),
                        companyId = ""
                    },
                    expiresIn = (int)(result.ExpiresAt - DateTime.UtcNow).TotalSeconds
                };
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, error = ex.Message });
            }
        }
        
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<object>>> Logout()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var result = await _authService.LogoutAsync(token);
                
                if (result)
                {
                    return Ok(new { success = true, message = "تم تسجيل الخروج بنجاح" });
                }
                
                throw new UnauthorizedException("فشل تسجيل الخروج");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }
        
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<object>>> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
                return Ok(new
                {
                    success = true,
                    token = result.Token,
                    expiresIn = (int)(result.ExpiresAt - DateTime.UtcNow).TotalSeconds
                });
            }
            catch (NotImplementedException)
            {
                throw new BadRequestException("ميزة Refresh Token غير متاحة حالياً");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw new UnauthorizedException("فشل تحديث Token");
            }
        }
        
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("غير مصرح لك بتنفيذ هذه العملية");
                }

                var result = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
                
                if (result)
                {
                    return Ok(new { success = true, message = "تم تغيير كلمة المرور بنجاح" });
                }
                
                throw new BadRequestException("فشل تغيير كلمة المرور");
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("validate-token")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<object>>> ValidateToken()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var isValid = await _authService.ValidateTokenAsync(token);
                
                if (isValid)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Token صالح"));
                }
                
                throw new UnauthorizedException("Token غير صالح أو منتهي الصلاحية");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                throw;
            }
        }

        /// <summary>
        /// Forgot Password - Request password reset token
        /// </summary>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    throw new BadRequestException(string.Join(", ", errors));
                }

                await _authService.ForgotPasswordAsync(dto.Email);

                _logger.LogInformation("Password reset requested for: {Email}", dto.Email);

                // Return generic success message to prevent user enumeration
                // Never reveal whether the email exists or not for security reasons
                return Ok(ApiResponse<object>.SuccessResponse(
                    null,
                    "إذا كان البريد الإلكتروني موجوداً، سيتم إرسال رابط إعادة تعيين كلمة المرور إلى بريدك الإلكتروني"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password for email: {Email}", dto.Email);
                // Return generic success message to prevent user enumeration
                return Ok(ApiResponse<object>.SuccessResponse(
                    null,
                    "إذا كان البريد الإلكتروني موجوداً، سيتم إرسال رابط إعادة تعيين كلمة المرور"
                ));
            }
        }

        /// <summary>
        /// Reset Password - Reset password using token
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    throw new BadRequestException(string.Join(", ", errors));
                }

                var result = await _authService.ResetPasswordAsync(dto.Email, dto.ResetToken, dto.NewPassword);

                if (result)
                {
                    _logger.LogInformation("Password reset successfully for: {Email}", dto.Email);
                    return Ok(ApiResponse<object>.SuccessResponse(
                        null,
                        "تم إعادة تعيين كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول باستخدام كلمة المرور الجديدة"
                    ));
                }

                throw new BadRequestException("فشل إعادة تعيين كلمة المرور. يرجى المحاولة مرة أخرى");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", dto.Email);
                throw;
            }
        }
    }
}
