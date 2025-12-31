using Shared.DTOS.Auth;

namespace BLL.ServiceAbstraction
{
    public interface IAuthService
    {
        // Register methods - separate for each role
        Task<AuthResponseDto> RegisterManagerAsync(RegisterManagerDto registerDto);
        Task<AuthResponseDto> RegisterEmployeeAsync(RegisterEmployeeDto registerDto, string managerUserId);
        Task<AuthResponseDto> RegisterAdminAsync(RegisterAdminDto registerDto);
        
        // Legacy method - kept for backward compatibility (optional)
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        
        // Login and validation
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> ValidateTokenAsync(string token);
        
        // Additional auth methods
        Task<bool> LogoutAsync(string token);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        // Forgot/Reset Password
        Task<string> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
    }
}

