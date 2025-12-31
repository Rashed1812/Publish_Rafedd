using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Auth
{
    public class RegisterAdminDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = null!;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }
}

