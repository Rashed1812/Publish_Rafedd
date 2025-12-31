using System.ComponentModel.DataAnnotations;

namespace Shared.DTOS.Reports
{
    public class CreateTaskReportDto
    {
        [Required]
        public int TaskItemId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string ReportText { get; set; } = null!;
    }
}

