using Shared.DTOS.Common;
using Shared.DTOS.Users;

namespace BLL.ServiceAbstraction
{
    public interface IManagerService
    {
        Task<PagedResponse<ManagerDto>> GetManagersAsync(ManagerFilterParams filterParams);
    }
}
