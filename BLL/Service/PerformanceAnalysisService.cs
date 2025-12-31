using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.AI;
using Shared.DTOS.Performance;
using System.Text.Json;

namespace BLL.Service
{
    public class PerformanceAnalysisService : IPerformanceAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeeklyPlanRepository _weeklyPlanRepository;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IGeminiAIService _geminiAIService;
        private readonly ILogger<PerformanceAnalysisService> _logger;

        public PerformanceAnalysisService(
            ApplicationDbContext context,
            IWeeklyPlanRepository weeklyPlanRepository,
            IPerformanceReportRepository performanceReportRepository,
            ITaskRepository taskRepository,
            IGeminiAIService geminiAIService,
            ILogger<PerformanceAnalysisService> logger)
        {
            _context = context;
            _weeklyPlanRepository = weeklyPlanRepository;
            _performanceReportRepository = performanceReportRepository;
            _taskRepository = taskRepository;
            _geminiAIService = geminiAIService;
            _logger = logger;
        }

        public async Task<PerformanceReportDto> GenerateWeeklyPerformanceReportAsync(int weeklyPlanId)
        {
            var weeklyPlan = await _weeklyPlanRepository.GetWithDetailsAsync(weeklyPlanId);

            if (weeklyPlan == null)
            {
                throw new InvalidOperationException("Weekly plan not found");
            }

            // Check if report already exists
            var existingReport = await _performanceReportRepository.GetByWeeklyPlanAsync(weeklyPlanId);

            if (existingReport != null)
            {
                return MapToResponseDto(existingReport, weeklyPlan);
            }

            // Collect employee reports for this week
            var tasks = await _taskRepository.GetByWeekForPerformanceAsync(
                weeklyPlan.Year, 
                weeklyPlan.Month, 
                weeklyPlan.WeekNumber);

            // Prepare data for AI analysis
            var employeeReports = new List<WeeklyPerformanceData>();

            // Group tasks by assigned employees (now using TaskAssignments)
            var allAssignments = tasks.SelectMany(t => t.Assignments ?? new List<DAL.Data.Models.TasksAndReports.TaskAssignment>()).ToList();
            var employeeGroups = allAssignments
                .GroupBy(a => a.EmployeeId);

            foreach (var group in employeeGroups)
            {
                var firstAssignment = group.First();
                var employee = firstAssignment.Employee;
                var employeeId = group.Key;

                // Get all tasks assigned to this employee
                var employeeTasks = tasks.Where(t => t.Assignments != null && t.Assignments.Any(a => a.EmployeeId == employeeId)).ToList();
                var completedTasks = employeeTasks.Where(t => t.IsCompleted).ToList();
                var reportTexts = employeeTasks
                    .SelectMany(t => t.Reports ?? new List<DAL.Data.Models.TasksAndReports.TaskReport>())
                    .Where(r => r.EmployeeId == employeeId)
                    .Select(r => r.ReportText)
                    .ToList();

                employeeReports.Add(new WeeklyPerformanceData
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee?.User?.FullName ?? "Unknown",
                    TaskCount = employeeTasks.Count,
                    CompletedTaskCount = completedTasks.Count,
                    ReportTexts = reportTexts,
                    WeeklyGoal = weeklyPlan.WeeklyGoal
                });
            }

            // Generate AI analysis
            _logger.LogInformation("Generating weekly performance analysis using Gemini AI for week {WeekId}", weeklyPlanId);
            var aiAnalysis = await _geminiAIService.AnalyzeWeeklyPerformanceAsync(
                weeklyPlanId,
                weeklyPlan.Year,
                weeklyPlan.Month,
                weeklyPlan.WeekNumber,
                employeeReports);

            // Calculate achievement percentage based on tasks
            var totalTasks = tasks.Count;
            var completedTasksCount = tasks.Count(t => t.IsCompleted);
            var achievementPercentage = totalTasks > 0 ? (float)(completedTasksCount * 100.0 / totalTasks) : 0;

            // Use AI percentage if provided, otherwise use calculated
            if (aiAnalysis.AchievementPercentage > 0)
            {
                achievementPercentage = aiAnalysis.AchievementPercentage;
            }

            // Create performance report
            var performanceReport = new PerformanceReport
            {
                WeeklyPlanId = weeklyPlanId,
                AchievementPercentage = achievementPercentage,
                Summary = aiAnalysis.Summary,
                Strengths = JsonSerializer.Serialize(aiAnalysis.Strengths),
                Weaknesses = JsonSerializer.Serialize(aiAnalysis.Weaknesses),
                Recommendations = JsonSerializer.Serialize(aiAnalysis.Recommendations),
                GeneratedAt = DateTime.UtcNow
            };

            await _performanceReportRepository.AddAsync(performanceReport);

            // Update weekly plan
            weeklyPlan.AchievementPercentage = achievementPercentage;
            weeklyPlan.ReportGeneratedAt = DateTime.UtcNow;

            _weeklyPlanRepository.Update(weeklyPlan);
            await _performanceReportRepository.SaveChangesAsync();

            return MapToResponseDto(performanceReport, weeklyPlan);
        }

        public async Task<PerformanceReportDto?> GetPerformanceReportAsync(int weeklyPlanId)
        {
            var performanceReport = await _performanceReportRepository.GetByWeeklyPlanAsync(weeklyPlanId);

            if (performanceReport == null)
            {
                return null;
            }

            return MapToResponseDto(performanceReport, performanceReport.WeeklyPlan);
        }

        public async Task<List<PerformanceReportDto>> GetPerformanceReportsByYearAsync(string managerUserId, int year)
        {
            var performanceReports = await _performanceReportRepository.GetByManagerAndYearAsync(managerUserId, year);

            return performanceReports
                .Select(pr => MapToResponseDto(pr, pr.WeeklyPlan))
                .ToList();
        }

        private PerformanceReportDto MapToResponseDto(PerformanceReport report, WeeklyPlan weeklyPlan)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return new PerformanceReportDto
            {
                Id = report.Id,
                WeeklyPlanId = report.WeeklyPlanId,
                WeekNumber = weeklyPlan.WeekNumber,
                WeekStartDate = weeklyPlan.WeekStartDate,
                WeekEndDate = weeklyPlan.WeekEndDate,
                AchievementPercentage = report.AchievementPercentage,
                Summary = report.Summary,
                Strengths = string.IsNullOrEmpty(report.Strengths) 
                    ? null 
                    : JsonSerializer.Deserialize<List<string>>(report.Strengths, options),
                Weaknesses = string.IsNullOrEmpty(report.Weaknesses) 
                    ? null 
                    : JsonSerializer.Deserialize<List<string>>(report.Weaknesses, options),
                Recommendations = string.IsNullOrEmpty(report.Recommendations) 
                    ? null 
                    : JsonSerializer.Deserialize<List<string>>(report.Recommendations, options),
                GeneratedAt = report.GeneratedAt
            };
        }
    }
}

