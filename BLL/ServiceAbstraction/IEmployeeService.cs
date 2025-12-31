using Shared.DTOS.Common;
using Shared.DTOS.Users;

namespace BLL.ServiceAbstraction
{
    public interface IEmployeeService
    {
        Task<PagedResponse<EmployeeDto>> GetEmployeesAsync(
            EmployeeFilterParams filterParams,
            string? managerUserId = null);
    }
}
