using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface ITaskRepository : IGenericRepository<TaskItem>
    {
        Task<List<TaskItem>> GetByWeekAsync(string managerUserId, int year, int month, int weekNumber);

        /// <summary>
        /// Gets all tasks assigned to a specific employee (via TaskAssignments table)
        /// </summary>
        Task<List<TaskItem>> GetByEmployeeAsync(int employeeId);

        Task<List<TaskItem>> GetByWeekForPerformanceAsync(int year, int month, int weekNumber);

        /// <summary>
        /// Gets a task with all assignments and reports eagerly loaded
        /// </summary>
        Task<TaskItem?> GetByIdWithReportsAsync(int taskId);

        /// <summary>
        /// Gets a task with all assignments eagerly loaded
        /// </summary>
        Task<TaskItem?> GetByIdWithAssignmentsAsync(int taskId);

        // NEW: For flexible filtering
        IQueryable<TaskItem> GetFilteredQueryable(string? managerUserId = null);
    }
}

