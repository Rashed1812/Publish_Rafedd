using BLL.ServiceAbstraction;
using DAL.Data.Models.NotificationsLogs;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.Extensions.Logging;
using Shared.DTOS.Notes;

namespace BLL.Service
{
    public class ImportantNoteService : IImportantNoteService
    {
        private readonly IImportantNoteRepository _noteRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly ILogger<ImportantNoteService> _logger;

        public ImportantNoteService(
            IImportantNoteRepository noteRepository,
            IEmployeeRepository employeeRepository,
            IManagerRepository managerRepository,
            ILogger<ImportantNoteService> logger)
        {
            _noteRepository = noteRepository;
            _employeeRepository = employeeRepository;
            _managerRepository = managerRepository;
            _logger = logger;
        }

        public async Task<List<ImportantNoteDto>> GetEmployeeNotesAsync(string employeeId)
        {
            var notes = await _noteRepository.GetByEmployeeIdAsync(employeeId);

            return notes.Select(n => new ImportantNoteDto
            {
                Id = n.Id,
                EmployeeId = n.EmployeeId,
                EmployeeName = n.Employee?.User?.FullName,
                Title = n.Title,
                Content = n.Content,
                WeekNumber = n.WeekNumber,
                Month = n.Month,
                Year = n.Year,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList();
        }

        public async Task<List<ImportantNoteDto>> GetManagerNotesAsync(string managerUserId)
        {
            var notes = await _noteRepository.GetByManagerIdAsync(managerUserId);

            return notes.Select(n => new ImportantNoteDto
            {
                Id = n.Id,
                EmployeeId = n.EmployeeId,
                EmployeeName = n.Employee?.User?.FullName,
                Title = n.Title,
                Content = n.Content,
                WeekNumber = n.WeekNumber,
                Month = n.Month,
                Year = n.Year,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList();
        }

        public async Task<ImportantNoteDto> GetNoteByIdAsync(int id, string userId)
        {
            var note = await _noteRepository.GetByIdAsync(id);

            if (note == null)
            {
                throw new InvalidOperationException("الملاحظة غير موجودة");
            }

            // Check if user is the employee who created it or their manager
            var employee = await _employeeRepository.GetByUserIdAsync(userId);
            var manager = await _managerRepository.GetByUserIdAsync(userId);

            bool isOwner = note.EmployeeId == userId;
            bool isManager = employee != null && note.EmployeeId == employee.UserId && employee.ManagerUserId == userId;
            bool isManagerOfEmployee = manager != null && note.Employee.ManagerUserId == userId;

            if (!isOwner && !isManager && !isManagerOfEmployee)
            {
                throw new UnauthorizedAccessException("غير مصرح لك بالوصول لهذه الملاحظة");
            }

            return new ImportantNoteDto
            {
                Id = note.Id,
                EmployeeId = note.EmployeeId,
                EmployeeName = note.Employee?.User?.FullName,
                Title = note.Title,
                Content = note.Content,
                WeekNumber = note.WeekNumber,
                Month = note.Month,
                Year = note.Year,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }

        public async Task<ImportantNoteDto> CreateNoteAsync(CreateImportantNoteDto dto, string employeeId)
        {
            // Verify employee exists
            var employee = await _employeeRepository.GetByUserIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("الموظف غير موجود");
            }

            var note = new ImportantNote
            {
                EmployeeId = employeeId,
                Title = dto.Title,
                Content = dto.Content,
                WeekNumber = dto.WeekNumber,
                Month = dto.Month,
                Year = dto.Year,
                CreatedAt = DateTime.UtcNow
            };

            await _noteRepository.AddAsync(note);
            await _noteRepository.SaveChangesAsync();

            _logger.LogInformation("Important note created: {NoteId} by Employee: {EmployeeId}", note.Id, employeeId);

            // Load employee data for response
            note = await _noteRepository.GetByIdAsync(note.Id);

            return new ImportantNoteDto
            {
                Id = note.Id,
                EmployeeId = note.EmployeeId,
                EmployeeName = note.Employee?.User?.FullName,
                Title = note.Title,
                Content = note.Content,
                WeekNumber = note.WeekNumber,
                Month = note.Month,
                Year = note.Year,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }

        public async Task<ImportantNoteDto> UpdateNoteAsync(int id, UpdateImportantNoteDto dto, string employeeId)
        {
            var note = await _noteRepository.GetByIdAsync(id);

            if (note == null)
            {
                throw new InvalidOperationException("الملاحظة غير موجودة");
            }

            // Only the employee who created it can update
            if (note.EmployeeId != employeeId)
            {
                throw new UnauthorizedAccessException("غير مصرح لك بتعديل هذه الملاحظة");
            }

            note.Title = dto.Title;
            note.Content = dto.Content;
            note.WeekNumber = dto.WeekNumber;
            note.Month = dto.Month;
            note.Year = dto.Year;
            note.UpdatedAt = DateTime.UtcNow;

            _noteRepository.Update(note);
            await _noteRepository.SaveChangesAsync();

            _logger.LogInformation("Important note updated: {NoteId} by Employee: {EmployeeId}", id, employeeId);

            // Reload for response
            note = await _noteRepository.GetByIdAsync(id);

            return new ImportantNoteDto
            {
                Id = note.Id,
                EmployeeId = note.EmployeeId,
                EmployeeName = note.Employee?.User?.FullName,
                Title = note.Title,
                Content = note.Content,
                WeekNumber = note.WeekNumber,
                Month = note.Month,
                Year = note.Year,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }

        public async Task<bool> DeleteNoteAsync(int id, string employeeId)
        {
            var note = await _noteRepository.GetByIdAsync(id);

            if (note == null)
            {
                throw new InvalidOperationException("الملاحظة غير موجودة");
            }

            // Only the employee who created it can delete
            if (note.EmployeeId != employeeId)
            {
                throw new UnauthorizedAccessException("غير مصرح لك بحذف هذه الملاحظة");
            }

            _noteRepository.Delete(note);
            await _noteRepository.SaveChangesAsync();

            _logger.LogInformation("Important note deleted: {NoteId} by Employee: {EmployeeId}", id, employeeId);

            return true;
        }
    }
}
