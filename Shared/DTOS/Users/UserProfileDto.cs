namespace Shared.DTOS.Users
{
    public class UserProfileDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string Role { get; set; } = null!;
        public string? CompanyId { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class UpdateUserProfileDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
    }
    
    public class EmployeeDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string CompanyId { get; set; } = null!;
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class EmployeeWithStatsDto : EmployeeDto
    {
        public EmployeeStatsDto? Stats { get; set; }
    }
    
    public class EmployeeStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalReports { get; set; }
        public double AveragePerformance { get; set; }
    }
    
    public class CreateEmployeeDto
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string Password { get; set; } = null!;
    }
    
    public class UpdateEmployeeDto
    {
        public string? Name { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
    }
}

