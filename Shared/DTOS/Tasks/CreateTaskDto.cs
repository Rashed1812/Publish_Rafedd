using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Tasks
{
    public class CreateTaskDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Range(1, 4)]
        public int WeekNumber { get; set; }

        /// <summary>
        /// List of employee IDs to assign this task to (supports multiple employees)
        /// </summary>
        public List<string> AssignedToEmployeeIds { get; set; } = new();
    }
}

