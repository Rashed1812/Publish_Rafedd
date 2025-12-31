namespace Shared.DTOS.Reports
{
    public class TaskReportDto
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public string TaskTitle { get; set; } = null!;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = null!;
        public string ReportText { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
    }
}

