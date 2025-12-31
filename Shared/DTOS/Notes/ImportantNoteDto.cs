namespace Shared.DTOS.Notes
{
    public class ImportantNoteDto
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = null!;
        public string? EmployeeName { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int? WeekNumber { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateImportantNoteDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int? WeekNumber { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class UpdateImportantNoteDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int? WeekNumber { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }
}
