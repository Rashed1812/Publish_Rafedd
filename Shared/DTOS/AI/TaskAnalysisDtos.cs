namespace Shared.DTOS.AI
{
    /// <summary>
    /// Request DTO for task analysis containing task details and employee updates
    /// </summary>
    public class TaskAnalysisRequestDto
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TaskUpdateDto> EmployeeUpdates { get; set; } = new();
    }

    /// <summary>
    /// Employee task update with name and timestamp for AI analysis
    /// </summary>
    public class TaskUpdateDto
    {
        public string EmployeeName { get; set; } = null!;
        public string ReportText { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// AI-generated task analysis result with all required fields
    /// </summary>
    public class TaskAnalysisResultDto
    {
        public int TaskId { get; set; }
        public int CompletionPercentage { get; set; }  // 0-100
        public string StatusSummary { get; set; } = null!;
        public List<string> DoneItems { get; set; } = new();
        public List<string> RemainingItems { get; set; } = new();
        public List<string> Blockers { get; set; } = new();
        public TaskOverallStatus OverallStatus { get; set; }
        public int DeadlineRiskScore { get; set; }  // 0-100 (0=no risk, 100=high risk)
        public List<string> SuggestedNextActions { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Overall task status enum
    /// </summary>
    public enum TaskOverallStatus
    {
        ON_TRACK,
        AT_RISK,
        DELAYED
    }
}
