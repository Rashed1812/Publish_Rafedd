using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.AIPlanning;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.AI;
using Shared.DTOS.Performance;
using System.Text.Json;

namespace BLL.Service
{
    public class MonthlyPerformanceAnalysisService : IMonthlyPerformanceAnalysisService
    {
        private readonly IMonthlyPlanRepository _monthlyPlanRepository;
        private readonly IWeeklyPlanRepository _weeklyPlanRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskReportRepository _taskReportRepository;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly IMonthlyPerformanceReportRepository _monthlyPerformanceReportRepository;
        private readonly IGeminiAIService _geminiAIService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MonthlyPerformanceAnalysisService> _logger;

        public MonthlyPerformanceAnalysisService(
            IMonthlyPlanRepository monthlyPlanRepository,
            IWeeklyPlanRepository weeklyPlanRepository,
            ITaskRepository taskRepository,
            ITaskReportRepository taskReportRepository,
            IPerformanceReportRepository performanceReportRepository,
            IMonthlyPerformanceReportRepository monthlyPerformanceReportRepository,
            IGeminiAIService geminiAIService,
            ApplicationDbContext context,
            ILogger<MonthlyPerformanceAnalysisService> logger)
        {
            _monthlyPlanRepository = monthlyPlanRepository;
            _context = context;
            _weeklyPlanRepository = weeklyPlanRepository;
            _taskRepository = taskRepository;
            _taskReportRepository = taskReportRepository;
            _performanceReportRepository = performanceReportRepository;
            _monthlyPerformanceReportRepository = monthlyPerformanceReportRepository;
            _geminiAIService = geminiAIService;
            _logger = logger;
        }

        public async Task<MonthlyPerformanceReportDto> GenerateMonthlyPerformanceReportAsync(int monthlyPlanId)
        {
            try
            {
                _logger.LogInformation("Starting monthly report generation for MonthlyPlanId: {MonthlyPlanId}", monthlyPlanId);

                // Check if report already exists
                var existingReport = await _monthlyPerformanceReportRepository.GetByMonthlyPlanIdAsync(monthlyPlanId);
                if (existingReport != null)
                {
                    throw new InvalidOperationException("تم إنشاء تقرير الأداء الشهري بالفعل لهذا الشهر");
                }

                // Get Monthly Plan
                var monthlyPlan = await _monthlyPlanRepository.GetByIdAsync(monthlyPlanId);
                if (monthlyPlan == null)
                {
                    throw new InvalidOperationException("الخطة الشهرية غير موجودة");
                }

                // Get all weekly plans for this month
                var weeklyPlans = await _weeklyPlanRepository.GetByMonthlyPlanIdAsync(monthlyPlanId);
                if (weeklyPlans.Count == 0)
                {
                    throw new InvalidOperationException("لا توجد خطط أسبوعية لهذا الشهر");
                }

                // ✅ Get all tasks for the month to extract manager ID
                var startDate = new DateTime(monthlyPlan.Year, monthlyPlan.Month, 1);
                var endDate = startDate.AddMonths(1);

                var sampleTasks = await _context.Tasks
                    .Where(t => t.CreatedAt >= startDate && t.CreatedAt < endDate)
                    .Take(1)
                    .ToListAsync();

                if (sampleTasks.Count == 0)
                {
                    throw new InvalidOperationException("لا توجد مهام لتحليلها في هذا الشهر");
                }

                // ✅ التصحيح هنا
                var managerUserId = sampleTasks.First().CreatedById;
                _logger.LogInformation("Manager ID found: {ManagerUserId}", managerUserId);

                // Aggregate data from all weeks
                int totalTasks = 0;
                int completedTasks = 0;
                var weeklyProgressList = new List<WeeklyProgressDto>();
                var allEmployeeReports = new List<WeeklyPerformanceData>();

                foreach (var weeklyPlan in weeklyPlans.OrderBy(wp => wp.WeekNumber))
                {
                    _logger.LogInformation("Processing week {WeekNumber}...", weeklyPlan.WeekNumber);

                    // Get tasks for this week with managerUserId
                    var tasks = await _taskRepository.GetByWeekForPerformanceAsync(
                        weeklyPlan.Year,
                        weeklyPlan.Month,
                        weeklyPlan.WeekNumber,
                        managerUserId
                    );

                    _logger.LogInformation("Found {TaskCount} tasks for week {WeekNumber}", tasks.Count, weeklyPlan.WeekNumber);

                    totalTasks += tasks.Count;
                    completedTasks += tasks.Count(t => t.IsCompleted);

                    // Get weekly performance report if exists
                    var weeklyPerformanceReport = await _performanceReportRepository.GetByWeeklyPlanAsync(weeklyPlan.Id);
                    float weeklyAchievement = weeklyPerformanceReport?.AchievementPercentage ?? 0;

                    weeklyProgressList.Add(new WeeklyProgressDto
                    {
                        WeekNumber = weeklyPlan.WeekNumber,
                        AchievementPercentage = weeklyAchievement
                    });

                    // Group tasks by employee to collect their performance data
                    var employeeTaskGroups = tasks
                        .SelectMany(t => t.Assignments ?? new List<DAL.Data.Models.TasksAndReports.TaskAssignment>())
                        .GroupBy(a => a.EmployeeId);

                    foreach (var group in employeeTaskGroups)
                    {
                        var firstAssignment = group.First();
                        var employee = firstAssignment.Employee;
                        var employeeId = group.Key;

                        // Get all tasks assigned to this employee for this week
                        var employeeTasks = tasks
                            .Where(t => t.Assignments != null && t.Assignments.Any(a => a.EmployeeId == employeeId))
                            .ToList();
                        var employeeCompletedTasks = employeeTasks.Count(t => t.IsCompleted);

                        // Get report texts from this employee
                        var reportTexts = employeeTasks
                            .SelectMany(t => t.Reports ?? new List<DAL.Data.Models.TasksAndReports.TaskReport>())
                            .Where(r => r.EmployeeId == employeeId)
                            .Select(r => r.ReportText)
                            .ToList();

                        allEmployeeReports.Add(new WeeklyPerformanceData
                        {
                            EmployeeId = employeeId,
                            EmployeeName = employee?.User?.FullName ?? "Unknown",
                            TaskCount = employeeTasks.Count,
                            CompletedTaskCount = employeeCompletedTasks,
                            ReportTexts = reportTexts,
                            WeeklyGoal = weeklyPlan.WeeklyGoal
                        });
                    }
                }

                // Calculate overall achievement percentage
                float achievementPercentage = totalTasks > 0 ? ((float)completedTasks / totalTasks) * 100 : 0;

                _logger.LogInformation("Total tasks: {TotalTasks}, Completed: {CompletedTasks}, Achievement: {Achievement}%",
                    totalTasks, completedTasks, achievementPercentage);

                // Call Gemini AI for monthly analysis
                _logger.LogInformation("Calling Gemini AI for analysis...");
                var aiAnalysis = await _geminiAIService.AnalyzeMonthlyPerformanceAsync(
                    monthlyPlanId,
                    monthlyPlan.Year,
                    monthlyPlan.Month,
                    monthlyPlan.MonthlyGoal,
                    weeklyProgressList,
                    allEmployeeReports,
                    totalTasks,
                    completedTasks,
                    achievementPercentage);

                // Create and save Monthly Performance Report
                _logger.LogInformation("Saving monthly performance report...");
                var monthlyPerformanceReport = new MonthlyPerformanceReport
                {
                    MonthlyPlanId = monthlyPlanId,
                    AchievementPercentage = achievementPercentage,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    Summary = aiAnalysis.Summary,
                    Strengths = JsonSerializer.Serialize(aiAnalysis.Strengths),
                    Weaknesses = JsonSerializer.Serialize(aiAnalysis.Weaknesses),
                    Recommendations = JsonSerializer.Serialize(aiAnalysis.Recommendations),
                    WeeklyProgressSummary = JsonSerializer.Serialize(weeklyProgressList),
                    GeneratedAt = DateTime.UtcNow
                };

                var savedReport = await _monthlyPerformanceReportRepository.CreateAsync(monthlyPerformanceReport);

                // Update MonthlyPlan with achievement percentage
                monthlyPlan.AchievementPercentage = achievementPercentage;
                _monthlyPlanRepository.Update(monthlyPlan);
                await _monthlyPlanRepository.SaveChangesAsync();

                _logger.LogInformation("Successfully generated monthly performance report for MonthlyPlan {MonthlyPlanId}: {Achievement}%",
                    monthlyPlanId, achievementPercentage);

                return MapToDto(savedReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly performance report for MonthlyPlanId: {MonthlyPlanId}", monthlyPlanId);
                throw;
            }
        }

        public async Task<MonthlyPerformanceReportDto?> GetMonthlyReportAsync(int monthlyPlanId)
        {
            var report = await _monthlyPerformanceReportRepository.GetByMonthlyPlanIdAsync(monthlyPlanId);
            return report != null ? MapToDto(report) : null;
        }

        public async Task<List<MonthlyPerformanceReportDto>> GetMonthlyReportsByYearAsync(string managerUserId, int year)
        {
            var reports = await _monthlyPerformanceReportRepository.GetByManagerUserIdAndYearAsync(managerUserId, year);
            return reports.Select(MapToDto).ToList();
        }

        private MonthlyPerformanceReportDto MapToDto(MonthlyPerformanceReport report)
        {
            return new MonthlyPerformanceReportDto
            {
                Id = report.Id,
                MonthlyPlanId = report.MonthlyPlanId,
                Month = report.MonthlyPlan.Month,
                Year = report.MonthlyPlan.Year,
                MonthlyGoal = report.MonthlyPlan.MonthlyGoal,
                AchievementPercentage = report.AchievementPercentage,
                TotalTasks = report.TotalTasks,
                CompletedTasks = report.CompletedTasks,
                Summary = report.Summary,
                Strengths = string.IsNullOrEmpty(report.Strengths)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(report.Strengths) ?? new List<string>(),
                Weaknesses = string.IsNullOrEmpty(report.Weaknesses)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(report.Weaknesses) ?? new List<string>(),
                Recommendations = string.IsNullOrEmpty(report.Recommendations)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(report.Recommendations) ?? new List<string>(),
                WeeklyProgress = string.IsNullOrEmpty(report.WeeklyProgressSummary)
                    ? new List<WeeklyProgressDto>()
                    : JsonSerializer.Deserialize<List<WeeklyProgressDto>>(report.WeeklyProgressSummary) ?? new List<WeeklyProgressDto>(),
                GeneratedAt = report.GeneratedAt
            };
        }
    }
}
