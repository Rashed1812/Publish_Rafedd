using DAL.Data;
using DAL.Data.Models.NotificationsLogs;
using DAL.Repositories.GenericRepositries;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.RepositoryClasses
{
    public class ImportantNoteRepository : GenericRepository<ImportantNote>, IImportantNoteRepository
    {
        public ImportantNoteRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<ImportantNote>> GetByEmployeeIdAsync(string employeeId)
        {
            return await _context.ImportantNotes
                .Include(i => i.Employee)
                    .ThenInclude(e => e.User)
                .Where(i => i.EmployeeId == employeeId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ImportantNote>> GetByManagerIdAsync(string managerUserId)
        {
            return await _context.ImportantNotes
                .Include(i => i.Employee)
                    .ThenInclude(e => e.User)
                .Where(i => i.Employee.ManagerUserId == managerUserId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ImportantNote>> GetByWeekAsync(string employeeId, int year, int month, int weekNumber)
        {
            return await _context.ImportantNotes
                .Include(i => i.Employee)
                    .ThenInclude(e => e.User)
                .Where(i => i.EmployeeId == employeeId &&
                           i.Year == year &&
                           i.Month == month &&
                           i.WeekNumber == weekNumber)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
    }
}
