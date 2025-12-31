using DAL.Data;
using DAL.Data.Models.TasksAndReports;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            IEmployeeRepository employeeRepository,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // GET /reports/daily (Manager)
        [HttpGet("daily")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetDailyReports(
            [FromQuery] string? date = null,
            [FromQuery] string? employeeId = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var managerUserId = GetUserId();
                var employees = await _employeeRepository.GetByManagerAsync(managerUserId);
                var employeeIds = employees.Select(e => e.UserId).ToList();

                DateTime targetDate;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
                {
                    targetDate = parsedDate.Date;
                }
                else
                {
                    targetDate = DateTime.UtcNow.Date;
                }

                var query = _context.TaskReports
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.TaskItem)
                    .Where(r => employeeIds.Contains(r.Employee.UserId) &&
                               r.SubmittedAt.Date == targetDate);

                if (!string.IsNullOrEmpty(employeeId))
                {
                    if (employeeIds.Contains(employeeId))
                    {
                        var employee = await _employeeRepository.GetByUserIdAsync(employeeId);
                        if (employee != null)
                        {
                            query = query.Where(r => r.EmployeeId == employee.Id);
                        }
                    }
                    else
                    {
                        throw new UnauthorizedException("غير مصرح لك بالوصول إلى تقارير هذا الموظف");
                    }
                }

                var total = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(total / (double)limit);
                var skip = (page - 1) * limit;

                var reports = await query
                    .OrderByDescending(r => r.SubmittedAt)
                    .Skip(skip)
                    .Take(limit)
                    .Select(r => new
                    {
                        id = r.Id.ToString(),
                        employeeId = r.Employee.UserId,
                        employeeName = r.Employee.User.FullName,
                        date = r.SubmittedAt,
                        summary = r.ReportText,
                        mood = "success", // You can add mood field to TaskReport model
                        attachments = Array.Empty<string>(),
                        createdAt = r.SubmittedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = reports,
                    pagination = new
                    {
                        page,
                        limit,
                        total,
                        totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily reports");
                throw;
            }
        }

        // POST /reports/daily (Employee)
        [HttpPost("daily")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<ActionResult<object>> CreateDailyReport([FromBody] CreateDailyReportDto dto)
        {
            try
            {
                var employeeUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee == null)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                DateTime reportDate;
                if (!string.IsNullOrEmpty(dto.Date) && DateTime.TryParse(dto.Date, out var parsedDate))
                {
                    reportDate = parsedDate;
                }
                else
                {
                    reportDate = DateTime.UtcNow;
                }

                // Create a task report (simplified - you might want to create a separate DailyReport model)
                var report = new TaskReport
                {
                    EmployeeId = employee.Id,
                    TaskItemId = 0, // You might want to create a general task for daily reports
                    ReportText = dto.Summary,
                    SubmittedAt = reportDate
                };

                _context.TaskReports.Add(report);
                await _context.SaveChangesAsync();

                var reportDto = new
                {
                    id = report.Id.ToString(),
                    employeeId = employeeUserId,
                    employeeName = employee.User.FullName,
                    date = report.SubmittedAt,
                    summary = report.ReportText,
                    mood = dto.Mood,
                    attachments = dto.Attachments ?? Array.Empty<string>(),
                    createdAt = report.SubmittedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "تم إرسال التقرير بنجاح",
                    data = reportDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating daily report");
                throw;
            }
        }

        // GET /reports/weekly (Manager)
        [HttpGet("weekly")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetWeeklyReport(
            [FromQuery] int weekNumber,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            try
            {
                var managerUserId = GetUserId();
                var currentDate = DateTime.UtcNow;
                var targetMonth = month ?? currentDate.Month;
                var targetYear = year ?? currentDate.Year;

                var employees = await _employeeRepository.GetByManagerAsync(managerUserId);
                var employeeIds = employees.Select(e => e.Id).ToList();

                var tasks = await _context.Tasks
                    .Include(t => t.Assignments)
                    .Where(t => t.Assignments.Any(a => employeeIds.Contains(a.EmployeeId)) &&
                               t.Year == targetYear &&
                               t.Month == targetMonth &&
                               t.WeekNumber == weekNumber)
                    .ToListAsync();

                var completedTasks = tasks.Where(t => t.IsCompleted).Select(t => t.Id.ToString()).ToList();
                var missedTasks = tasks.Where(t => !t.IsCompleted && t.Deadline.HasValue && t.Deadline.Value < DateTime.UtcNow)
                    .Select(t => t.Id.ToString()).ToList();

                var goalCompletion = tasks.Count > 0 
                    ? (int)((double)completedTasks.Count / tasks.Count * 100) 
                    : 0;

                var employeeStats = employees.Select(e => new
                {
                    employeeId = e.UserId,
                    employeeName = e.User.FullName,
                    completedTasks = tasks.Count(t => t.Assignments.Any(a => a.EmployeeId == e.Id) && t.IsCompleted),
                    totalTasks = tasks.Count(t => t.Assignments.Any(a => a.EmployeeId == e.Id)),
                    completionRate = tasks.Count(t => t.Assignments.Any(a => a.EmployeeId == e.Id)) > 0
                        ? (int)((double)tasks.Count(t => t.Assignments.Any(a => a.EmployeeId == e.Id) && t.IsCompleted) / tasks.Count(t => t.Assignments.Any(a => a.EmployeeId == e.Id)) * 100)
                        : 0
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = $"weekly-report-{weekNumber}-{targetMonth}-{targetYear}",
                        weekNumber = weekNumber,
                        month = targetMonth,
                        year = targetYear,
                        goalCompletion = goalCompletion,
                        completedTasks = completedTasks,
                        missedTasks = missedTasks,
                        supervisorNotes = "",
                        generatedAt = DateTime.UtcNow,
                        employeeStats = employeeStats
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly report");
                throw;
            }
        }
    }

    public class CreateDailyReportDto
    {
        public string? Date { get; set; }
        public string Summary { get; set; } = null!;
        public string Mood { get; set; } = "success"; // success, average, failed
        public string[]? Attachments { get; set; }
    }
}

