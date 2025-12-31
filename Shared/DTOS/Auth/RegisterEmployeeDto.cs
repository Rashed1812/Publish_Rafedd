using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Auth
{
    public class RegisterEmployeeDto
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

        [Required]
        [MaxLength(100)]
        public string Position { get; set; } = null!; // منصب الموظف
    }
}

