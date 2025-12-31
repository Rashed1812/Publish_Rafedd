using DAL.Data.Models.NotificationsLogs;
using DAL.Repositories.GenericRepositries;

namespace DAL.Repositories.RepositoryIntrfaces
{
    public interface IImportantNoteRepository : IGenericRepository<ImportantNote>
    {
        Task<List<ImportantNote>> GetByEmployeeIdAsync(string employeeId);
        Task<List<ImportantNote>> GetByManagerIdAsync(string managerUserId);
        Task<List<ImportantNote>> GetByWeekAsync(string employeeId, int year, int month, int weekNumber);
    }
}
