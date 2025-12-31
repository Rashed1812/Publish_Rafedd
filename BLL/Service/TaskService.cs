using System.Linq.Expressions;
using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Data.Models.TasksAndReports;
using DAL.Extensions;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Common;
using Shared.DTOS.Tasks;

namespace BLL.Service
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskRepository _taskRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<TaskService> _logger;

        public TaskService(
            ApplicationDbContext context,
            ITaskRepository taskRepository,
            IManagerRepository managerRepository,
            IEmployeeRepository employeeRepository,
            ILogger<TaskService> logger)
        {
            _context = context;
            _taskRepository = taskRepository;
            _managerRepository = managerRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<TaskDto> CreateTaskAsync(string managerUserId, CreateTaskDto dto)
        {
            var manager = await _managerRepository.GetByUserIdAsync(managerUserId);

            if (manager == null)
            {
                throw new InvalidOperationException("Manager not found");
            }

            // Verify all assigned employees exist and belong to this manager
            if (dto.AssignedToEmployeeIds != null && dto.AssignedToEmployeeIds.Any())
            {
                foreach (var employeeId in dto.AssignedToEmployeeIds)
                {
                    var employee = await _employeeRepository.GetByIdAsync(employeeId);

                    if (employee == null || !employee.IsActive || employee.ManagerUserId != managerUserId)
                    {
                        throw new InvalidOperationException($"Employee with ID {employeeId} not found or not assigned to this manager");
                    }
                }
            }

            // Create the task
            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Deadline = dto.Deadline,
                CreatedById = managerUserId,
                Year = dto.Year,
                Month = dto.Month,
                WeekNumber = dto.WeekNumber,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            // Create task assignments for each employee
            if (dto.AssignedToEmployeeIds != null && dto.AssignedToEmployeeIds.Any())
            {
                foreach (var employeeId in dto.AssignedToEmployeeIds)
                {
                    var assignment = new TaskAssignment
                    {
                        TaskItemId = task.Id,
                        EmployeeId = employeeId,
                        AssignedByUserId = managerUserId,
                        AssignedAt = DateTime.UtcNow
                    };

                    await _context.TaskAssignments.AddAsync(assignment);
                }

                await _context.SaveChangesAsync();

                // Reload task with assignments
                task = await _taskRepository.GetByIdWithAssignmentsAsync(task.Id) ?? task;
            }

            return await MapToTaskDtoAsync(task);
        }

        public async Task<List<TaskDto>> GetTasksByWeekAsync(string managerUserId, int year, int month, int weekNumber)
        {
            var tasks = await _taskRepository.GetByWeekAsync(managerUserId, year, month, weekNumber);

            var taskDtos = new List<TaskDto>();
            foreach (var task in tasks)
            {
                taskDtos.Add(await MapToTaskDtoAsync(task));
            }

            return taskDtos;
        }

        public async Task<List<TaskDto>> GetEmployeeTasksAsync(string employeeUserId)
        {
            var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found");
            }

            var tasks = await _taskRepository.GetByEmployeeAsync(employee.Id);

            var taskDtos = new List<TaskDto>();
            foreach (var task in tasks)
            {
                taskDtos.Add(await MapToTaskDtoAsync(task));
            }

            return taskDtos;
        }

        public async Task<TaskDto> UpdateTaskStatusAsync(int taskId, bool isCompleted, string? completedByUserId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);

            if (task == null)
            {
                throw new InvalidOperationException("Task not found");
            }

            task.IsCompleted = isCompleted;
            task.CompletedAt = isCompleted ? DateTime.UtcNow : null;

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();

            return await MapToTaskDtoAsync(task);
        }

        public async Task<bool> DeleteTaskAsync(int taskId, string managerUserId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);

            if (task == null || task.CreatedById != managerUserId)
            {
                return false;
            }

            _taskRepository.Delete(task);
            await _taskRepository.SaveChangesAsync();

            return true;
        }

        private async Task<TaskDto> MapToTaskDtoAsync(TaskItem task)
        {
            // Map assigned employees
            var assignedEmployees = task.Assignments?
                .Select(a => new AssignedEmployeeDto
                {
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee?.User?.FullName ?? "Unknown",
                    AssignedAt = a.AssignedAt
                })
                .ToList() ?? new List<AssignedEmployeeDto>();

            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CreatedAt = task.CreatedAt,
                Deadline = task.Deadline,
                Year = task.Year,
                Month = task.Month,
                WeekNumber = task.WeekNumber,
                AssignedEmployees = assignedEmployees,
                IsCompleted = task.IsCompleted,
                CompletedAt = task.CompletedAt,
                ReportsCount = task.Reports?.Count ?? 0
            };
        }

        // NEW: For flexible filtering
        public async Task<PagedResponse<TaskDto>> GetTasksAsync(TaskFilterParams filterParams, string? managerUserId = null)
        {
            var query = _taskRepository.GetFilteredQueryable(managerUserId);

            // Apply temporal filters (year, month, week)
            if (filterParams.Year.HasValue)
            {
                query = query.Where(t => t.Year == filterParams.Year.Value);
            }

            if (filterParams.Month.HasValue)
            {
                query = query.Where(t => t.Month == filterParams.Month.Value);
            }

            if (filterParams.WeekNumber.HasValue)
            {
                query = query.Where(t => t.WeekNumber == filterParams.WeekNumber.Value);
            }

            // Apply date range filters
            if (filterParams.StartDate.HasValue || filterParams.EndDate.HasValue)
            {
                query = query.ApplyDateRange(
                    filterParams.StartDate,
                    filterParams.EndDate,
                    t => t.CreatedAt);
            }

            // Apply deadline range filters
            if (filterParams.DeadlineFrom.HasValue)
            {
                query = query.Where(t => t.Deadline >= filterParams.DeadlineFrom.Value);
            }

            if (filterParams.DeadlineTo.HasValue)
            {
                query = query.Where(t => t.Deadline <= filterParams.DeadlineTo.Value);
            }

            // Apply employee filter (checks if task is assigned to specific employee)
            if (!string.IsNullOrEmpty(filterParams.EmployeeId))
            {
                // First, get the employee's ID from their user ID
                var employee = await _employeeRepository.GetByUserIdAsync(filterParams.EmployeeId);
                if (employee != null)
                {
                    var employeeId = employee.Id;
                    query = query.Where(t => t.Assignments.Any(a => a.EmployeeId == employeeId));
                }
            }

            // Apply completion status filter
            if (filterParams.IsCompleted.HasValue)
            {
                query = query.Where(t => t.IsCompleted == filterParams.IsCompleted.Value);
            }

            // Apply optional search
            if (!string.IsNullOrEmpty(filterParams.Search))
            {
                var searchLower = filterParams.Search.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchLower) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchLower)));
            }

            // Define sortable fields
            var sortExpressions = new Dictionary<string, Expression<Func<TaskItem, object>>>
            {
                ["createdat"] = t => t.CreatedAt,
                ["deadline"] = t => t.Deadline ?? DateTime.MaxValue,
                ["title"] = t => t.Title,
                ["weeknumber"] = t => t.WeekNumber,
                ["completedat"] = t => t.CompletedAt ?? DateTime.MaxValue
            };

            // Apply sorting with default (createdAt desc)
            query = query.ApplySorting(
                filterParams.SortBy,
                filterParams.IsDescending,
                sortExpressions,
                defaultSort: t => t.CreatedAt);

            // Get paged results
            var (items, totalCount) = await query.ToPagedListAsync(
                filterParams.GetPage(),
                filterParams.GetPageSize());

            // Map to DTOs
            var dtos = items.Select(t => new TaskDto
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

            return PagedResponse<TaskDto>.Create(
                dtos,
                totalCount,
                filterParams.GetPage(),
                filterParams.GetPageSize());
        }
    }
}

