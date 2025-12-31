using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOS.Common;
using Shared.DTOS.Users;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmployeeRepository employeeRepository,
            IManagerRepository managerRepository,
            ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _employeeRepository = employeeRepository;
            _managerRepository = managerRepository;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // GET /users/me
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
        {
            try
            {
                var userId = GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new NotFoundException("المستخدم غير موجود");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault()?.ToLower() ?? "employee";

                string? companyId = null;
                string? position = null;
                string? department = null;

                if (role == "manager")
                {
                    var manager = await _managerRepository.GetByUserIdAsync(userId);
                    if (manager != null)
                    {
                        companyId = manager.Id.ToString();
                        position = "مدير عام"; // You can add Position field to Manager model if needed
                    }
                }
                else if (role == "employee")
                {
                    var employee = await _employeeRepository.GetByUserIdAsync(userId);
                    if (employee != null)
                    {
                        companyId = employee.ManagerUserId;
                        position = employee.Position;
                        department = employee.Department;
                    }
                }

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Email = user.Email!,
                    Phone = user.PhoneNumber,
                    Role = role,
                    CompanyId = companyId,
                    Position = position,
                    Department = department,
                    Avatar = null, // Add Avatar field to ApplicationUser if needed
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                throw;
            }
        }

        // PUT /users/me
        [HttpPut("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateCurrentUser([FromBody] UpdateUserProfileDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new NotFoundException("المستخدم غير موجود");
                }

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    user.FullName = dto.Name;
                }

                if (!string.IsNullOrEmpty(dto.Phone))
                {
                    user.PhoneNumber = dto.Phone;
                }

                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    throw new BadRequestException($"فشل تحديث الملف الشخصي: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault()?.ToLower() ?? "employee";

                string? companyId = null;
                string? position = null;
                string? department = null;

                if (role == "manager")
                {
                    var manager = await _managerRepository.GetByUserIdAsync(userId);
                    companyId = manager?.Id.ToString();
                    position = "مدير عام";
                }
                else if (role == "employee")
                {
                    var employee = await _employeeRepository.GetByUserIdAsync(userId);
                    companyId = employee?.ManagerUserId;
                    position = employee?.Position;
                    department = employee?.Department;
                }

                var updatedProfile = new UserProfileDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Email = user.Email!,
                    Phone = user.PhoneNumber,
                    Role = role,
                    CompanyId = companyId,
                    Position = position,
                    Department = department,
                    Avatar = dto.Avatar,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(new
                {
                    success = true,
                    user = updatedProfile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current user");
                throw;
            }
        }

        // GET /users/employees (Manager only)
        [HttpGet("employees")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var managerUserId = GetUserId();
                var employees = await _employeeRepository.GetByManagerAsync(managerUserId);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    employees = employees.Where(e =>
                        e.User.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        e.User.Email!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (e.User.PhoneNumber != null && e.User.PhoneNumber.Contains(search)) ||
                        (e.Position != null && e.Position.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (e.Department != null && e.Department.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var total = employees.Count;
                var totalPages = (int)Math.Ceiling(total / (double)limit);
                var skip = (page - 1) * limit;

                var employeesDto = employees
                    .Skip(skip)
                    .Take(limit)
                    .Select(e => new EmployeeDto
                    {
                        Id = e.UserId,
                        Name = e.User.FullName,
                        Email = e.User.Email!,
                        Phone = e.User.PhoneNumber ?? "",
                        Role = "employee",
                        CompanyId = e.ManagerUserId,
                        Department = e.Department,
                        Position = e.Position,
                        Avatar = null,
                        CreatedAt = e.User.CreatedAt
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = employeesDto,
                    pagination = new
                    {
                        page,
                        limit,
                        total,
                        totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees");
                throw;
            }
        }

        // GET /users/employees/:id
        [HttpGet("employees/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeWithStatsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<ActionResult<ApiResponse<EmployeeWithStatsDto>>> GetEmployeeById(string id)
        {
            var managerUserId = GetUserId();
            var employee = await _employeeRepository.GetByUserIdAsync(id);

            if (employee == null || employee.ManagerUserId != managerUserId)
            {
                throw new NotFoundException("الموظف غير موجود");
            }

            // Verify User relationship is loaded
            if (employee.User == null)
            {
                _logger.LogError("Employee {EmployeeId} found but User relationship is null", id);
                throw new NotFoundException("بيانات الموظف غير مكتملة");
            }

            // Get stats (simplified - you can enhance this with actual task/report counts)
            var totalTasks = await _context.Tasks
                .CountAsync(t => t.Assignments.Any(a => a.EmployeeId == employee.Id));
            var completedTasks = await _context.Tasks
                .CountAsync(t => t.Assignments.Any(a => a.EmployeeId == employee.Id) && t.IsCompleted);
            var totalReports = await _context.TaskReports
                .CountAsync(r => r.EmployeeId == employee.Id);

            var employeeDto = new EmployeeWithStatsDto
            {
                Id = employee.UserId,
                Name = employee.User.FullName,
                Email = employee.User.Email!,
                Phone = employee.User.PhoneNumber ?? "",
                Role = "employee",
                CompanyId = employee.ManagerUserId,
                Department = employee.Department,
                Position = employee.Position,
                Avatar = null,
                CreatedAt = employee.User.CreatedAt,
                Stats = new EmployeeStatsDto
                {
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    TotalReports = totalReports,
                    AveragePerformance = totalTasks > 0 ? (completedTasks * 100.0 / totalTasks) : 0
                }
            };

            return Ok(new
            {
                success = true,
                data = employeeDto
            });
        }

        // POST /users/employees
        [HttpPost("employees")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<EmployeeDto>>> AddEmployee([FromBody] CreateEmployeeDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    throw new BadRequestException("البريد الإلكتروني مستخدم بالفعل");
                }

                // Create user
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FullName = dto.Name,
                    PhoneNumber = dto.Phone,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    throw new BadRequestException($"فشل إنشاء المستخدم: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Assign Employee role
                await _userManager.AddToRoleAsync(user, "Employee");

                // Create Employee profile
                var employee = new Employee
                {
                    UserId = user.Id,
                    ManagerUserId = managerUserId,
                    Position = dto.Position,
                    Department = dto.Department,
                    IsActive = true
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                var employeeDto = new EmployeeDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Email = user.Email!,
                    Phone = user.PhoneNumber ?? "",
                    Role = "employee",
                    CompanyId = managerUserId,
                    Department = dto.Department,
                    Position = dto.Position,
                    Avatar = null,
                    CreatedAt = user.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "تم إضافة الموظف بنجاح",
                    data = employeeDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                throw;
            }
        }

        // PUT /users/employees/:id
        [HttpPut("employees/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(id);
                
                if (employee == null || employee.ManagerUserId != managerUserId)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    throw new NotFoundException("المستخدم غير موجود");
                }

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    user.FullName = dto.Name;
                }

                if (!string.IsNullOrEmpty(dto.Position))
                {
                    employee.Position = dto.Position;
                }

                if (!string.IsNullOrEmpty(dto.Department))
                {
                    employee.Department = dto.Department;
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                _employeeRepository.Update(employee);
                await _context.SaveChangesAsync();

                var employeeDto = new EmployeeDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Email = user.Email!,
                    Phone = user.PhoneNumber ?? "",
                    Role = "employee",
                    CompanyId = employee.ManagerUserId,
                    Department = employee.Department,
                    Position = employee.Position,
                    Avatar = null,
                    CreatedAt = user.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث بيانات الموظف بنجاح",
                    data = employeeDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                throw;
            }
        }

        // DELETE /users/employees/:id
        [HttpDelete("employees/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteEmployee(string id)
        {
            try
            {
                var managerUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(id);
                
                if (employee == null || employee.ManagerUserId != managerUserId)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                // Soft delete
                employee.IsActive = false;
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }

                _employeeRepository.Update(employee);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم حذف الموظف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                throw;
            }
        }
    }
}

