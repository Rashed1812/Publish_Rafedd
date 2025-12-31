using System.Linq.Expressions;
using BLL.ServiceAbstraction;
using DAL.Data.Models.IdentityModels;
using DAL.Extensions;
using DAL.Repositories.RepositoryIntrfaces;
using Shared.DTOS.Common;
using Shared.DTOS.Users;

namespace BLL.Service
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<PagedResponse<EmployeeDto>> GetEmployeesAsync(
            EmployeeFilterParams filterParams,
            string? managerUserId = null)
        {
            var query = _employeeRepository.GetFilteredQueryable(managerUserId);

            // Apply optional filters
            if (filterParams.IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == filterParams.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(filterParams.Department))
            {
                query = query.Where(e => e.Department == filterParams.Department);
            }

            // Apply optional search
            if (!string.IsNullOrEmpty(filterParams.Search))
            {
                var searchLower = filterParams.Search.ToLower();
                query = query.Where(e =>
                    e.User.FullName.ToLower().Contains(searchLower) ||
                    e.User.Email.ToLower().Contains(searchLower) ||
                    (e.User.PhoneNumber != null && e.User.PhoneNumber.Contains(searchLower)) ||
                    e.Position.ToLower().Contains(searchLower));
            }

            // Define sortable fields
            var sortExpressions = new Dictionary<string, Expression<Func<Employee, object>>>
            {
                ["name"] = e => e.User.FullName,
                ["position"] = e => e.Position,
                ["department"] = e => e.Department ?? "",
                ["createdat"] = e => e.CreatedAt
            };

            // Apply sorting with default (createdAt desc)
            query = query.ApplySorting(
                filterParams.SortBy,
                filterParams.IsDescending,
                sortExpressions,
                defaultSort: e => e.CreatedAt);

            // Get paged results
            var (items, totalCount) = await query.ToPagedListAsync(
                filterParams.GetPage(),
                filterParams.GetPageSize());

            // Map to DTOs
            var dtos = items.Select(e => new EmployeeDto
            {
                Id = e.UserId,
                Name = e.User.FullName,
                Email = e.User.Email,
                Phone = e.User.PhoneNumber ?? "",
                Role = "employee",
                CompanyId = e.ManagerUserId,
                Position = e.Position,
                Department = e.Department,
                CreatedAt = e.CreatedAt
            }).ToList();

            return PagedResponse<EmployeeDto>.Create(
                dtos,
                totalCount,
                filterParams.GetPage(),
                filterParams.GetPageSize());
        }
    }
}
