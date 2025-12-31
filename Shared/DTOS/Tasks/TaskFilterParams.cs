using Shared.DTOS.Common;

namespace Shared.DTOS.Tasks
{
    public class TaskFilterParams : BaseFilterParams
    {
        // Optional time filters
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? WeekNumber { get; set; }

        // Optional date range
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DeadlineFrom { get; set; }
        public DateTime? DeadlineTo { get; set; }

        // Optional filters
        public string? EmployeeId { get; set; }
        public bool? IsCompleted { get; set; }
        public string? Search { get; set; }

        // Sortable fields: createdAt, deadline, title, weekNumber, completedAt
    }
}
