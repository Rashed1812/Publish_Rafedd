using Shared.DTOS.Common;

namespace Shared.DTOS.Users
{
    public class EmployeeFilterParams : BaseFilterParams
    {
        // Optional filters
        public string? Search { get; set; }
        public string? ManagerId { get; set; }
        public string? Department { get; set; }
        public bool? IsActive { get; set; }

        // Sortable fields: name, position, department, createdAt
    }
}
