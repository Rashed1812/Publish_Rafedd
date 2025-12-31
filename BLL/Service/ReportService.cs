using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Reports;

namespace BLL.Service
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskReportRepository _taskReportRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            ITaskReportRepository taskReportRepository,
            ITaskRepository taskRepository,
            IEmployeeRepository employeeRepository,
            ILogger<ReportService> logger)
        {
            _context = context;
            _taskReportRepository = taskReportRepository;
            _taskRepository = taskRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<TaskReportDto> CreateTaskReportAsync(string employeeUserId, CreateTaskReportDto dto)
        {
            var employee = await _employeeRepository.GetWithDetailsAsync(employeeUserId);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found");
            }

            var task = await _taskRepository.GetByIdWithAssignmentsAsync(dto.TaskItemId);

            if (task == null)
            {
                throw new InvalidOperationException("Task not found");
            }

            // Verify employee is assigned to this task
            var isAssigned = task.Assignments?.Any(a => a.EmployeeId == employee.Id) ?? false;
            if (!isAssigned)
            {
                throw new UnauthorizedAccessException("Employee is not assigned to this task");
            }

            var report = new TaskReport
            {
                TaskItemId = dto.TaskItemId,
                EmployeeId = employee.Id,
                ReportText = dto.ReportText,
                SubmittedAt = DateTime.UtcNow
            };

            await _taskReportRepository.AddAsync(report);
            await _taskReportRepository.SaveChangesAsync();

            return await MapToReportDtoAsync(report);
        }

        public async Task<List<TaskReportDto>> GetReportsByTaskAsync(int taskId)
        {
            var reports = await _taskReportRepository.GetByTaskAsync(taskId);

            var reportDtos = new List<TaskReportDto>();
            foreach (var report in reports)
            {
                reportDtos.Add(await MapToReportDtoAsync(report));
            }

            return reportDtos;
        }

        public async Task<List<TaskReportDto>> GetReportsByWeekAsync(string managerUserId, int year, int month, int weekNumber)
        {
            var reports = await _taskReportRepository.GetByWeekAsync(managerUserId, year, month, weekNumber);

            var reportDtos = new List<TaskReportDto>();
            foreach (var report in reports)
            {
                reportDtos.Add(await MapToReportDtoAsync(report));
            }

            return reportDtos;
        }

        private async Task<TaskReportDto> MapToReportDtoAsync(TaskReport report)
        {
            return new TaskReportDto
            {
                Id = report.Id,
                TaskItemId = report.TaskItemId,
                TaskTitle = report.TaskItem.Title,
                EmployeeId = report.EmployeeId,
                EmployeeName = report.Employee.User.FullName,
                ReportText = report.ReportText,
                SubmittedAt = report.SubmittedAt
            };
        }
    }
}

