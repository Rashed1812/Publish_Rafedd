using Shared.DTOS.AI;
using Shared.DTOS.Performance;

namespace BLL.ServiceAbstraction
{
    public interface IGeminiAIService
    {
        Task<AIPlanResponseDto> GenerateAnnualPlanAsync(string arabicTarget, int year);
        Task<AIPerformanceAnalysisDto> AnalyzeWeeklyPerformanceAsync(
            int weeklyPlanId,
            int year,
            int month,
            int weekNumber,
            List<WeeklyPerformanceData> employeeReports);
        Task<AIPerformanceAnalysisDto> AnalyzeMonthlyPerformanceAsync(
            int monthlyPlanId,
            int year,
            int month,
            string monthlyGoal,
            List<WeeklyProgressDto> weeklyProgress,
            List<WeeklyPerformanceData> allEmployeeReports,
            int totalTasks,
            int completedTasks,
            float achievementPercentage);
    }

    public class WeeklyPerformanceData
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = null!;
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public List<string> ReportTexts { get; set; } = new();
        public string WeeklyGoal { get; set; } = null!;
    }
}

