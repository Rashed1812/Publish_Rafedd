using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Auth
{
    public class RegisterDto
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
        public string Role { get; set; } = null!; // Admin, Manager, Employee

        // Manager-specific
        public string? CompanyName { get; set; }
        public string? BusinessType { get; set; }
        public string? BusinessDescription { get; set; }

        // Employee-specific
        public string? ManagerUserId { get; set; }
    }
}

