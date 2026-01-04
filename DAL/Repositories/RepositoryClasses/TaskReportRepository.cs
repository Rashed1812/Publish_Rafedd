using DAL.Data;
using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class TaskReportRepository : GenericRepository<TaskReport>, ITaskReportRepository
    {
        public TaskReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<TaskReport>> GetByTaskAsync(int taskId)
        {
            return await _dbSet
                .Include(tr => tr.TaskItem)
                .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                .Where(tr => tr.TaskItemId == taskId)
                .OrderByDescending(tr => tr.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<TaskReport>> GetByWeekAsync(string managerUserId, int year, int month, int weekNumber)
        {
            return await _dbSet
                .Include(tr => tr.TaskItem)
                .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                .Where(tr => tr.TaskItem.CreatedById == managerUserId &&
                           tr.TaskItem.Year == year &&
                           tr.TaskItem.Month == month &&
                           tr.TaskItem.WeekNumber == weekNumber)
                .OrderByDescending(tr => tr.SubmittedAt)
                .ToListAsync();
        }
        public async Task<List<TaskReport>> GetByMonthAsync(string managerUserId, int year, int month)
        {
            return await _dbSet
                .Include(tr => tr.TaskItem)
                .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                .Where(tr => tr.TaskItem.CreatedById == managerUserId &&
                           tr.TaskItem.Year == year &&
                           tr.TaskItem.Month == month)
                .OrderByDescending(tr => tr.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<TaskReport>> GetByEmployeeAsync(int employeeId)
        {
            return await _dbSet
                .Include(tr => tr.TaskItem)
                .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                .Where(tr => tr.EmployeeId == employeeId)
                .OrderByDescending(tr => tr.SubmittedAt)
                .ToListAsync();
        }
    }
}

