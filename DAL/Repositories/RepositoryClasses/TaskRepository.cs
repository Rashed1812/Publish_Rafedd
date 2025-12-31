using DAL.Data;
using DAL.Data.Models.TasksAndReports;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<TaskItem>> GetByWeekAsync(string managerUserId, int year, int month, int weekNumber)
        {
            return await _dbSet
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.Reports)
                .Where(t => t.CreatedById == managerUserId &&
                           t.Year == year &&
                           t.Month == month &&
                           t.WeekNumber == weekNumber)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetByEmployeeAsync(int employeeId)
        {
            return await _dbSet
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.Reports)
                .Where(t => t.Assignments.Any(a => a.EmployeeId == employeeId))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetByWeekForPerformanceAsync(int year, int month, int weekNumber)
        {
            return await _dbSet
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.Reports)
                    .ThenInclude(r => r.Employee)
                        .ThenInclude(e => e.User)
                .Where(t => t.Year == year &&
                           t.Month == month &&
                           t.WeekNumber == weekNumber)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetByIdWithReportsAsync(int taskId)
        {
            return await _dbSet
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.Reports)
                    .ThenInclude(r => r.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<TaskItem?> GetByIdWithAssignmentsAsync(int taskId)
        {
            return await _dbSet
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        // NEW: For flexible filtering
        public IQueryable<TaskItem> GetFilteredQueryable(string? managerUserId = null)
        {
            var query = _dbSet
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.Reports)
                .Include(t => t.CreatedBy)
                .AsQueryable();

            // Optional manager filter
            if (!string.IsNullOrEmpty(managerUserId))
            {
                query = query.Where(t => t.CreatedById == managerUserId);
            }

            return query;
        }
    }
}

