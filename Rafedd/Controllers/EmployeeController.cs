using BLL.ServiceAbstraction;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOS.Common;
using Shared.DTOS.Employee;
using Shared.DTOS.Reports;
using Shared.DTOS.Tasks;
using Shared.Exceptions;
using System.Globalization;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/employee")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class EmployeeController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IReportService _reportService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskReportRepository _taskReportRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            ITaskService taskService,
            IReportService reportService,
            IEmployeeRepository employeeRepository,
            ITaskRepository taskRepository,
            ITaskReportRepository taskReportRepository,
            INotificationService notificationService,
            ILogger<EmployeeController> logger)
        {
            _taskService = taskService;
            _reportService = reportService;
            _employeeRepository = employeeRepository;
            _taskRepository = taskRepository;
            _taskReportRepository = taskReportRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // Dashboard
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDashboardDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDashboardDto>), 404)]
        public async Task<ActionResult<ApiResponse<EmployeeDashboardDto>>> GetDashboard()
        {
            var employeeUserId = GetUserId();

            var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
            if (employee == null)
            {
                throw new NotFoundException("الموظف غير موجود");
            }

            var allTasks = await _taskRepository.GetByEmployeeAsync(employee.Id);
            var totalTasks = allTasks.Count;
            var completedTasks = allTasks.Count(t => t.IsCompleted);
            var pendingTasks = totalTasks - completedTasks;
            var completionPercentage = totalTasks > 0 ? (int)((completedTasks / (double)totalTasks) * 100) : 0;

            var allReports = await _taskReportRepository.GetByEmployeeAsync(employee.Id);

            var now = DateTime.UtcNow;
            var currentCulture = CultureInfo.CurrentCulture;
            var weekNumber = currentCulture.Calendar.GetWeekOfYear(
                now,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Sunday);

            var reportsThisWeek = allReports.Count(r =>
                r.SubmittedAt.Year == now.Year &&
                currentCulture.Calendar.GetWeekOfYear(r.SubmittedAt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday) == weekNumber);

            var averageScore = completionPercentage;

            string performanceLevel = averageScore switch
            {
                >= 90 => "ممتاز",
                >= 75 => "جيد جداً",
                >= 60 => "جيد",
                >= 50 => "مقبول",
                _ => "ضعيف"
            };

            var dashboard = new EmployeeDashboardDto
            {
                TaskStats = new TaskStatsDto
                {
                    Total = totalTasks,
                    Completed = completedTasks,
                    Pending = pendingTasks,
                    CompletionPercentage = completionPercentage
                },
                ReportStats = new ReportStatsDto
                {
                    TotalReports = allReports.Count,
                    ReportsThisWeek = reportsThisWeek
                },
                Performance = new PerformanceDto
                {
                    AverageScore = Math.Round((double)averageScore, 2),
                    PerformanceLevel = performanceLevel
                },
                CurrentWeek = new CurrentWeekInfoDto
                {
                    Year = now.Year,
                    Month = now.Month,
                    WeekNumber = weekNumber
                }
            };

            return Ok(ApiResponse<EmployeeDashboardDto>.SuccessResponse(dashboard, "تم الحصول على لوحة التحكم بنجاح"));
        }

        // Weekly Report
        [HttpGet("weekly-report")]
        [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), 404)]
        public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetWeeklyReport(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] int weekNumber)
        {
            var employeeUserId = GetUserId();

            var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
            if (employee == null)
            {
                throw new NotFoundException("الموظف غير موجود");
            }

            var tasks = await _taskRepository.GetByWeekForPerformanceAsync(year, month, weekNumber);

            var employeeTasks = tasks
                .Where(t => t.Assignments.Any(a => a.EmployeeId == employee.Id))
                .ToList();

            var taskDtos = employeeTasks.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Deadline = t.Deadline,
                Year = t.Year,
                Month = t.Month,
                WeekNumber = t.WeekNumber,
                AssignedEmployees = t.Assignments.Select(a => new AssignedEmployeeDto
                {
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee.User.FullName,
                    AssignedAt = a.AssignedAt
                }).ToList(),
                IsCompleted = t.IsCompleted,
                CompletedAt = t.CompletedAt,
                ReportsCount = t.Reports?.Count ?? 0
            }).ToList();

            return Ok(ApiResponse<List<TaskDto>>.SuccessResponse(
                taskDtos,
                $"تم الحصول على مهام الأسبوع {weekNumber} بنجاح"));
        }

        // Tasks
        [HttpGet("tasks")]
        [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), 400)]
        public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetMyTasks()
        {
            try
            {
                var employeeUserId = GetUserId();
                var result = await _taskService.GetEmployeeTasksAsync(employeeUserId);
                return Ok(ApiResponse<List<TaskDto>>.SuccessResponse(result, "تم الحصول على المهام بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPut("tasks/{taskId}/complete")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 400)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> CompleteTask(int taskId)
        {
            try
            {
                var employeeUserId = GetUserId();
                var result = await _taskService.UpdateTaskStatusAsync(taskId, true, employeeUserId);

                // إشعار للمدير عند إتمام المهمة
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee != null && !string.IsNullOrEmpty(employee.ManagerUserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        employee.ManagerUserId,
                        "task_completed",
                        "مهمة مكتملة",
                        $"قام {employee.User.FullName} بإتمام المهمة: {result.Title}",
                        "medium",
                        $"/tasks/{taskId}",
                        taskId.ToString()
                    );
                }

                return Ok(ApiResponse<TaskDto>.SuccessResponse(result, "تم تحديث حالة المهمة إلى مكتملة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPut("tasks/{taskId}/incomplete")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 400)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> MarkTaskIncomplete(int taskId)
        {
            try
            {
                var employeeUserId = GetUserId();
                var result = await _taskService.UpdateTaskStatusAsync(taskId, false, employeeUserId);

                // إشعار للمدير عند إلغاء إتمام المهمة
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee != null && !string.IsNullOrEmpty(employee.ManagerUserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        employee.ManagerUserId,
                        "task_status_changed",
                        "تغيير حالة المهمة",
                        $"قام {employee.User.FullName} بتغيير حالة المهمة: {result.Title} إلى غير مكتملة",
                        "low",
                        $"/tasks/{taskId}",
                        taskId.ToString()
                    );
                }

                return Ok(ApiResponse<TaskDto>.SuccessResponse(result, "تم تحديث حالة المهمة إلى غير مكتملة بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        // Reports
        [HttpPost("reports")]
        [ProducesResponseType(typeof(ApiResponse<TaskReportDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TaskReportDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<TaskReportDto>), 401)]
        public async Task<ActionResult<ApiResponse<TaskReportDto>>> CreateReport([FromBody] CreateTaskReportDto dto)
        {
            try
            {
                var employeeUserId = GetUserId();
                var result = await _reportService.CreateTaskReportAsync(employeeUserId, dto);

                // إشعار للمدير عند تقديم تقرير
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee != null && !string.IsNullOrEmpty(employee.ManagerUserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        employee.ManagerUserId,
                        "report_submitted",
                        "تقرير جديد",
                        $"قام {employee.User.FullName} بتقديم تقرير جديد للمهمة #{dto.TaskItemId}",
                        "high",
                        $"/reports/{result.Id}",
                        result.Id.ToString()
                    );
                }

                return Ok(ApiResponse<TaskReportDto>.SuccessResponse(result, "تم إنشاء التقرير بنجاح"));
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
        }

        [HttpGet("tasks/{taskId}/reports")]
        [ProducesResponseType(typeof(ApiResponse<List<TaskReportDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<TaskReportDto>>>> GetTaskReports(int taskId)
        {
            var result = await _reportService.GetReportsByTaskAsync(taskId);
            return Ok(ApiResponse<List<TaskReportDto>>.SuccessResponse(result, "تم الحصول على تقارير المهمة بنجاح"));
        }
    }
}