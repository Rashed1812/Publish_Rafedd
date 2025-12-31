using Shared.DTOS.AI;

namespace BLL.ServiceAbstraction
{
    public interface IGeminiService
    {
        /// <summary>
        /// Generates annual plan breakdown from a single goal
        /// </summary>
        Task<AnnualPlanGenerationDto> GenerateAnnualPlanAsync(string goal, int year);

        /// <summary>
        /// Generates monthly performance analysis and recommendations
        /// </summary>
        Task<MonthlyReportGenerationDto> GenerateMonthlyReportAsync(MonthlyReportRequestDto request);

        /// <summary>
        /// Generates AI insights for employee performance
        /// </summary>
        Task<string> GeneratePerformanceInsightsAsync(EmployeePerformanceDataDto data);

        /// <summary>
        /// Analyzes task progress by comparing original requirements with employee updates
        /// </summary>
        Task<TaskAnalysisResultDto> AnalyzeTaskProgressAsync(TaskAnalysisRequestDto request);
    }
}
