using Shared.DTOS.Reports;

namespace BLL.ServiceAbstraction
{
    public interface IReportService
    {
        Task<TaskReportDto> CreateTaskReportAsync(string employeeUserId, CreateTaskReportDto dto);
        Task<List<TaskReportDto>> GetReportsByTaskAsync(int taskId);
        Task<List<TaskReportDto>> GetReportsByWeekAsync(string managerUserId, int year, int month, int weekNumber);
    }
}

