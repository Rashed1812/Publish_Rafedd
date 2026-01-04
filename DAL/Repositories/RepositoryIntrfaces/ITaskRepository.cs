using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface ITaskRepository : IGenericRepository<TaskItem>
    {
        Task<List<TaskItem>> GetByWeekAsync(string managerUserId, int year, int month, int weekNumber);
        Task<List<TaskItem>> GetByMonthAsync(string managerUserId, int year, int month);
        Task<List<TaskItem>> GetByEmployeeAsync(int employeeId);
        Task<List<TaskItem>> GetByWeekForPerformanceAsync(int year, int month, int weekNumber, string? managerUserId = null);
        Task<TaskItem?> GetByIdWithReportsAsync(int taskId);
        Task<TaskItem?> GetByIdWithAssignmentsAsync(int taskId);
        IQueryable<TaskItem> GetFilteredQueryable(string? managerUserId = null);
    }
}

