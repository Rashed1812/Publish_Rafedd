using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        public string Email { get; set; } = string.Empty;
    }
}
