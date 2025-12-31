using Shared.DTOS.Common;
using Shared.DTOS.Tasks;

namespace BLL.ServiceAbstraction
{
    public interface ITaskService
    {
        Task<TaskDto> CreateTaskAsync(string managerUserId, CreateTaskDto dto);
        Task<List<TaskDto>> GetTasksByWeekAsync(string managerUserId, int year, int month, int weekNumber);
        Task<List<TaskDto>> GetEmployeeTasksAsync(string employeeUserId);
        Task<TaskDto> UpdateTaskStatusAsync(int taskId, bool isCompleted, string? completedByUserId);
        Task<bool> DeleteTaskAsync(int taskId, string managerUserId);

        // NEW: For flexible filtering
        Task<PagedResponse<TaskDto>> GetTasksAsync(TaskFilterParams filterParams, string? managerUserId = null);
    }
}

