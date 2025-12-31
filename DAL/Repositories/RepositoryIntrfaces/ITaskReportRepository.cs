using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface ITaskReportRepository : IGenericRepository<TaskReport>
    {
        Task<List<TaskReport>> GetByTaskAsync(int taskId);
        Task<List<TaskReport>> GetByWeekAsync(string managerUserId, int year, int month, int weekNumber);
        Task<List<TaskReport>> GetByEmployeeAsync(int employeeId);
    }
}

