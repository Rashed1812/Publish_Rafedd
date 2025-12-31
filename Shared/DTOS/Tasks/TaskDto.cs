namespace Shared.DTOS.Tasks
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int WeekNumber { get; set; }

        /// <summary>
        /// List of employees assigned to this task
        /// </summary>
        public List<AssignedEmployeeDto> AssignedEmployees { get; set; } = new List<AssignedEmployeeDto>();

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ReportsCount { get; set; }
    }

    public class AssignedEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = null!;
        public DateTime AssignedAt { get; set; }
    }
}

