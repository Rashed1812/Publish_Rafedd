using DAL.Data.Models.IdentityModels;

namespace DAL.Data.Models.TasksAndReports
{
    /// <summary>
    /// Join table for many-to-many relationship between Tasks and Employees
    /// Allows a single task to be assigned to multiple employees
    /// </summary>
    public class TaskAssignment
    {
        public int Id { get; set; }

        // Foreign Keys
        public int TaskItemId { get; set; }
        public int EmployeeId { get; set; }

        // Navigation Properties
        public TaskItem TaskItem { get; set; } = null!;
        public Employee Employee { get; set; } = null!;

        // Metadata
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByUserId { get; set; } = null!;
        public ApplicationUser AssignedBy { get; set; } = null!;
    }
}
