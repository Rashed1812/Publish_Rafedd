using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Auth
{
    public class RegisterManagerDto
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
        [MaxLength(200)]
        public string CompanyName { get; set; } = null!;

        [MaxLength(100)]
        public string? BusinessType { get; set; }

        [MaxLength(1000)]
        public string? BusinessDescription { get; set; }

        [Required]
        public int SubscriptionPlanId { get; set; } // خطة الاشتراك التي يريدها المدير
    }
}

