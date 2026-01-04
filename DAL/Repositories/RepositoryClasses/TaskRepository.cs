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

        public async Task<List<TaskItem>> GetByWeekForPerformanceAsync(int year, int month, int weekNumber, string? managerUserId = null)
        {
            var weekRange = DateHelper.GetWeekRange(year, month, weekNumber);

            var query = _context.Tasks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(t => t.Reports)
                .Where(t => t.CreatedAt >= weekRange.Start &&
                            t.CreatedAt < weekRange.End);
            if (!string.IsNullOrEmpty(managerUserId))
            {
                query = query.Where(t => t.CreatedById == managerUserId);
            }

            return await query
                .OrderByDescending(t => t.CreatedAt)
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
        public async Task<List<TaskItem>> GetByMonthAsync(string managerUserId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await _context.Tasks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Where(t => t.CreatedById == managerUserId &&
                            t.CreatedAt >= startDate &&
                            t.CreatedAt < endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
    public static class DateHelper
    {
        public static (DateTime Start, DateTime End) GetWeekRange(int year, int month, int weekNumber)
        {
            if (weekNumber < 1 || weekNumber > 5)
            {
                throw new ArgumentException("رقم الأسبوع يجب أن يكون بين 1 و 5", nameof(weekNumber));
            }
            var firstDayOfMonth = new DateTime(year, month, 1);
            var startDate = firstDayOfMonth.AddDays((weekNumber - 1) * 7);
            if (startDate.Month != month)
            {
                startDate = firstDayOfMonth;
            }

            var endDate = startDate.AddDays(7);


            var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month)).AddDays(1);
            if (endDate > lastDayOfMonth)
            {
                endDate = lastDayOfMonth;
            }

            return (startDate, endDate);
        }


        public static int GetWeekNumber(DateTime date)
        {
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var daysDifference = (date.Date - firstDayOfMonth).Days;
            var weekNumber = (daysDifference / 7) + 1;


            return Math.Min(weekNumber, 5);
        }

        public static int GetWeeksInMonth(int year, int month)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            return (int)Math.Ceiling(daysInMonth / 7.0);
        }
    }
}

