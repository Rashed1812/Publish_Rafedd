using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOS.Tasks;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskService taskService,
            ApplicationDbContext context,
            IEmployeeRepository employeeRepository,
            ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _context = context;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // GET /tasks/weekly (Manager)
        [HttpGet("weekly")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetWeeklyTasks(
            [FromQuery] int? weekNumber = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null,
            [FromQuery] string? employeeId = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var managerUserId = GetUserId();
                var currentDate = DateTime.UtcNow;
                var targetMonth = month ?? currentDate.Month;
                var targetYear = year ?? currentDate.Year;

                var query = _context.Tasks
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.Employee)
                            .ThenInclude(e => e.User)
                    .Include(t => t.CreatedBy)
                    .Where(t => t.Year == targetYear &&
                               t.Month == targetMonth &&
                               t.CreatedBy.Id == managerUserId);

                if (weekNumber.HasValue)
                {
                    query = query.Where(t => t.WeekNumber == weekNumber.Value);
                }

                if (!string.IsNullOrEmpty(employeeId))
                {
                    var employee = await _employeeRepository.GetByUserIdAsync(employeeId);
                    if (employee != null && employee.ManagerUserId == managerUserId)
                    {
                        query = query.Where(t => t.Assignments.Any(a => a.EmployeeId == employee.Id));
                    }
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var statusBool = status == "completed";
                    query = query.Where(t => t.IsCompleted == statusBool);
                }

                var tasks = await query
                    .Select(t => new
                    {
                        id = t.Id.ToString(),
                        employeeId = t.Assignments.FirstOrDefault() != null ? t.Assignments.FirstOrDefault()!.Employee.UserId : null,
                        employeeName = t.Assignments.FirstOrDefault() != null ? t.Assignments.FirstOrDefault()!.Employee.User.FullName : null,
                        weekNumber = t.WeekNumber,
                        month = t.Month,
                        year = t.Year,
                        goal = t.Description ?? "",
                        task = t.Title,
                        status = t.IsCompleted ? "completed" : (t.Deadline.HasValue && t.Deadline.Value < DateTime.UtcNow ? "in-progress" : "pending"),
                        createdAt = t.CreatedAt,
                        completedAt = t.CompletedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = tasks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly tasks");
                throw;
            }
        }

        // GET /tasks/weekly/me (Employee)
        [HttpGet("weekly/me")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetMyWeeklyTasks(
            [FromQuery] int? weekNumber = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            try
            {
                var employeeUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee == null)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                var currentDate = DateTime.UtcNow;
                var targetMonth = month ?? currentDate.Month;
                var targetYear = year ?? currentDate.Year;

                var query = _context.Tasks
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.Employee)
                            .ThenInclude(e => e.User)
                    .Where(t => t.Assignments.Any(a => a.EmployeeId == employee.Id) &&
                               t.Year == targetYear &&
                               t.Month == targetMonth);

                if (weekNumber.HasValue)
                {
                    query = query.Where(t => t.WeekNumber == weekNumber.Value);
                }

                var tasks = await query
                    .Select(t => new
                    {
                        id = t.Id.ToString(),
                        employeeId = t.Assignments.FirstOrDefault() != null ? t.Assignments.FirstOrDefault()!.Employee.UserId : null,
                        employeeName = t.Assignments.FirstOrDefault() != null ? t.Assignments.FirstOrDefault()!.Employee.User.FullName : null,
                        weekNumber = t.WeekNumber,
                        month = t.Month,
                        year = t.Year,
                        goal = t.Description ?? "",
                        task = t.Title,
                        status = t.IsCompleted ? "completed" : (t.Deadline.HasValue && t.Deadline.Value < DateTime.UtcNow ? "in-progress" : "pending"),
                        createdAt = t.CreatedAt,
                        completedAt = t.CompletedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = tasks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee weekly tasks");
                throw;
            }
        }

        // POST /tasks/weekly (Employee)
        [HttpPost("weekly")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<ActionResult<object>> CreateWeeklyTask([FromBody] CreateWeeklyTaskDto dto)
        {
            try
            {
                var employeeUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee == null)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                var task = new TaskItem
                {
                    Title = dto.Task,
                    Description = dto.Goal,
                    WeekNumber = dto.WeekNumber,
                    Month = dto.Month,
                    Year = dto.Year,
                    CreatedById = employeeUserId,
                    IsCompleted = dto.Status == "completed",
                    CreatedAt = DateTime.UtcNow
                };

                if (dto.Status == "completed")
                {
                    task.CompletedAt = DateTime.UtcNow;
                }

                _context.Tasks.Add(task);

                // Create task assignment
                var assignment = new TaskAssignment
                {
                    TaskItem = task,
                    EmployeeId = employee.Id,
                    AssignedByUserId = employeeUserId,
                    AssignedAt = DateTime.UtcNow
                };
                _context.Set<TaskAssignment>().Add(assignment);

                await _context.SaveChangesAsync();

                var taskDto = new
                {
                    id = task.Id.ToString(),
                    employeeId = employeeUserId,
                    employeeName = employee.User.FullName,
                    weekNumber = task.WeekNumber,
                    month = task.Month,
                    year = task.Year,
                    goal = task.Description,
                    task = task.Title,
                    status = dto.Status,
                    createdAt = task.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "تم إضافة المهمة بنجاح",
                    data = taskDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weekly task");
                throw;
            }
        }

        // PUT /tasks/weekly/:id (Employee)
        [HttpPut("weekly/{id}")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<ActionResult<object>> UpdateWeeklyTask(int id, [FromBody] UpdateWeeklyTaskDto dto)
        {
            try
            {
                var employeeUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                if (employee == null)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                var task = await _context.Tasks
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.Id == id && t.Assignments.Any(a => a.EmployeeId == employee.Id));

                if (task == null)
                {
                    throw new NotFoundException("المهمة غير موجودة");
                }

                if (!string.IsNullOrEmpty(dto.Status))
                {
                    task.IsCompleted = dto.Status == "completed";
                    if (dto.Status == "completed" && !task.CompletedAt.HasValue)
                    {
                        task.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;
                    }
                    else if (dto.Status != "completed")
                    {
                        task.CompletedAt = null;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث المهمة بنجاح",
                    data = new
                    {
                        id = task.Id.ToString(),
                        status = task.IsCompleted ? "completed" : "pending",
                        completedAt = task.CompletedAt,
                        updatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weekly task");
                throw;
            }
        }
    }

    public class CreateWeeklyTaskDto
    {
        public int WeekNumber { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Goal { get; set; } = null!;
        public string Task { get; set; } = null!;
        public string Status { get; set; } = "pending";
    }

    public class UpdateWeeklyTaskDto
    {
        public string? Status { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

