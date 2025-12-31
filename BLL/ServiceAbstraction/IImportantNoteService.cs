using Shared.DTOS.Notes;

namespace BLL.ServiceAbstraction
{
    public interface IImportantNoteService
    {
        Task<List<ImportantNoteDto>> GetEmployeeNotesAsync(string employeeId);
        Task<List<ImportantNoteDto>> GetManagerNotesAsync(string managerUserId);
        Task<ImportantNoteDto> GetNoteByIdAsync(int id, string userId);
        Task<ImportantNoteDto> CreateNoteAsync(CreateImportantNoteDto dto, string employeeId);
        Task<ImportantNoteDto> UpdateNoteAsync(int id, UpdateImportantNoteDto dto, string employeeId);
        Task<bool> DeleteNoteAsync(int id, string employeeId);
    }
}
