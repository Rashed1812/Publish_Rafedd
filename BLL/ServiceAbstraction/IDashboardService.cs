using Shared.DTOS.Dashboard;

namespace BLL.ServiceAbstraction
{
    public interface IDashboardService
    {
        Task<ManagerDashboardDto> GetManagerDashboardAsync(string managerUserId);
    }
}

