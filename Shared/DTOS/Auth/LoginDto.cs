using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Auth
{
    public class LoginDto
    {
        [Required]
        public string EmailOrPhone { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
    
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
    
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;
        
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = null!;
        
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

