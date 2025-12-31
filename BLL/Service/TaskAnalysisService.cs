using BLL.ServiceAbstraction;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.Extensions.Logging;
using Shared.DTOS.AI;
using Shared.Exceptions;

namespace BLL.Service
{
    public class TaskAnalysisService : ITaskAnalysisService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<TaskAnalysisService> _logger;

        public TaskAnalysisService(
            ITaskRepository taskRepository,
            IGeminiService geminiService,
            ILogger<TaskAnalysisService> logger)
        {
            _taskRepository = taskRepository;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<TaskAnalysisResultDto> AnalyzeTaskAsync(int taskId, string managerUserId)
        {
            try
            {
                // Fetch task with all reports
                var task = await _taskRepository.GetByIdWithReportsAsync(taskId);

                if (task == null)
                {
                    throw new NotFoundException($"المهمة برقم {taskId} غير موجودة");
                }

                // Verify manager owns this task
                if (task.CreatedById != managerUserId)
                {
                    throw new UnauthorizedAccessException("ليس لديك صلاحية لتحليل هذه المهمة");
                }

                // Build analysis request
                var request = new TaskAnalysisRequestDto
                {
                    TaskId = task.Id,
                    TaskTitle = task.Title,
                    TaskDescription = task.Description,
                    Deadline = task.Deadline,
                    CreatedAt = task.CreatedAt,
                    EmployeeUpdates = task.Reports
                        .OrderBy(r => r.SubmittedAt)
                        .Select(r => new TaskUpdateDto
                        {
                            EmployeeName = r.Employee?.User?.FullName ?? "موظف",
                            ReportText = r.ReportText,
                            SubmittedAt = r.SubmittedAt
                        })
                        .ToList()
                };

                // Call Gemini for analysis
                var result = await _geminiService.AnalyzeTaskProgressAsync(request);

                _logger.LogInformation("Successfully analyzed task {TaskId} for manager {ManagerId}",
                    taskId, managerUserId);

                return result;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing task {TaskId} for manager {ManagerId}",
                    taskId, managerUserId);
                throw;
            }
        }

        public async Task<List<TaskAnalysisResultDto>> AnalyzeBatchTasksAsync(List<int> taskIds, string managerUserId)
        {
            var results = new List<TaskAnalysisResultDto>();

            foreach (var taskId in taskIds)
            {
                try
                {
                    var analysis = await AnalyzeTaskAsync(taskId, managerUserId);
                    results.Add(analysis);
                }
                catch (NotFoundException ex)
                {
                    _logger.LogWarning(ex, "Task {TaskId} not found during batch analysis", taskId);
                    // Continue with other tasks
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Unauthorized access to task {TaskId} during batch analysis", taskId);
                    // Continue with other tasks
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to analyze task {TaskId} during batch analysis", taskId);
                    // Continue with other tasks
                }
            }

            return results;
        }
    }
}
