using DAL.Data;
using DAL.Data.Models;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/costs")]
    [Authorize]
    public class CostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<CostsController> _logger;

        public CostsController(
            ApplicationDbContext context,
            IEmployeeRepository employeeRepository,
            ILogger<CostsController> logger)
        {
            _context = context;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // GET /costs/weekly (Manager)
        [HttpGet("weekly")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetWeeklyCosts(
            [FromQuery] int weekNumber,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null,
            [FromQuery] string? employeeId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var managerUserId = GetUserId();
                var currentDate = DateTime.UtcNow;
                var targetMonth = month ?? currentDate.Month;
                var targetYear = year ?? currentDate.Year;

                var query = _context.WeeklyCosts
                    .Include(c => c.Employee)
                        .ThenInclude(e => e.User)
                    .Where(c => c.WeekNumber == weekNumber &&
                               c.Month == targetMonth &&
                               c.Year == targetYear);

                // Filter by employee if manager
                if (!string.IsNullOrEmpty(employeeId))
                {
                    var employee = await _employeeRepository.GetByUserIdAsync(employeeId);
                    if (employee != null && employee.ManagerUserId == managerUserId)
                    {
                        query = query.Where(c => c.EmployeeId == employeeId);
                    }
                    else
                    {
                        throw new UnauthorizedException("غير مصرح لك بالوصول إلى بيانات هذا الموظف");
                    }
                }
                else
                {
                    // Get all employees for this manager
                    var employees = await _employeeRepository.GetByManagerAsync(managerUserId);
                    var employeeIds = employees.Select(e => e.UserId).ToList();
                    query = query.Where(c => employeeIds.Contains(c.EmployeeId));
                }

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var total = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(total / (double)limit);
                var skip = (page - 1) * limit;

                var costs = await query
                    .Skip(skip)
                    .Take(limit)
                    .Select(c => new
                    {
                        id = c.Id.ToString(),
                        employeeId = c.EmployeeId,
                        employeeName = c.Employee.User.FullName,
                        weekNumber = c.WeekNumber,
                        month = c.Month,
                        year = c.Year,
                        description = c.Description,
                        amount = c.Amount,
                        costType = c.CostType,
                        status = c.Status,
                        createdAt = c.CreatedAt,
                        paidAt = c.PaidAt
                    })
                    .ToListAsync();

                // Calculate summary
                var allCosts = await query.ToListAsync();
                var summary = new
                {
                    total = allCosts.Sum(c => (double)c.Amount),
                    paid = allCosts.Where(c => c.Status == "paid").Sum(c => (double)c.Amount),
                    pending = allCosts.Where(c => c.Status == "pending").Sum(c => (double)c.Amount)
                };

                return Ok(new
                {
                    success = true,
                    data = costs,
                    pagination = new
                    {
                        page,
                        limit,
                        total,
                        totalPages
                    },
                    summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly costs");
                throw;
            }
        }

        // GET /costs/weekly/me (Employee)
        [HttpGet("weekly/me")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetMyWeeklyCosts(
            [FromQuery] int weekNumber,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            try
            {
                var employeeUserId = GetUserId();
                var currentDate = DateTime.UtcNow;
                var targetMonth = month ?? currentDate.Month;
                var targetYear = year ?? currentDate.Year;

                var costs = await _context.WeeklyCosts
                    .Include(c => c.Employee)
                        .ThenInclude(e => e.User)
                    .Where(c => c.EmployeeId == employeeUserId &&
                               c.WeekNumber == weekNumber &&
                               c.Month == targetMonth &&
                               c.Year == targetYear)
                    .Select(c => new
                    {
                        id = c.Id.ToString(),
                        employeeId = c.EmployeeId,
                        employeeName = c.Employee.User.FullName,
                        weekNumber = c.WeekNumber,
                        month = c.Month,
                        year = c.Year,
                        description = c.Description,
                        amount = c.Amount,
                        costType = c.CostType,
                        status = c.Status,
                        createdAt = c.CreatedAt,
                        paidAt = c.PaidAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = costs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee weekly costs");
                throw;
            }
        }

        // POST /costs/weekly (Manager)
        [HttpPost("weekly")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<ActionResult<object>> CreateWeeklyCost([FromBody] CreateWeeklyCostDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                
                // Verify employee belongs to manager
                var employee = await _employeeRepository.GetByUserIdAsync(dto.EmployeeId);
                if (employee == null || employee.ManagerUserId != managerUserId)
                {
                    throw new UnauthorizedException("غير مصرح لك بإضافة تكلفة لهذا الموظف");
                }

                var cost = new WeeklyCost
                {
                    EmployeeId = dto.EmployeeId,
                    WeekNumber = dto.WeekNumber,
                    Month = dto.Month,
                    Year = dto.Year,
                    Description = dto.Description,
                    Amount = dto.Amount,
                    CostType = dto.CostType,
                    Status = dto.Status ?? "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.WeeklyCosts.Add(cost);
                await _context.SaveChangesAsync();

                var costDto = new
                {
                    id = cost.Id.ToString(),
                    employeeId = cost.EmployeeId,
                    employeeName = employee.User.FullName,
                    weekNumber = cost.WeekNumber,
                    month = cost.Month,
                    year = cost.Year,
                    description = cost.Description,
                    amount = cost.Amount,
                    costType = cost.CostType,
                    status = cost.Status,
                    createdAt = cost.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "تم إضافة التكلفة بنجاح",
                    data = costDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weekly cost");
                throw;
            }
        }

        // PUT /costs/weekly/:id (Manager)
        [HttpPut("weekly/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<ActionResult<object>> UpdateWeeklyCost(int id, [FromBody] UpdateWeeklyCostDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                var cost = await _context.WeeklyCosts
                    .Include(c => c.Employee)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cost == null)
                {
                    throw new NotFoundException("التكلفة غير موجودة");
                }

                // Verify employee belongs to manager
                if (cost.Employee.ManagerUserId != managerUserId)
                {
                    throw new UnauthorizedException("غير مصرح لك بتحديث هذه التكلفة");
                }

                if (!string.IsNullOrEmpty(dto.Status))
                {
                    cost.Status = dto.Status;
                    if (dto.Status == "paid" && dto.PaidAt.HasValue)
                    {
                        cost.PaidAt = dto.PaidAt.Value;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث التكلفة بنجاح",
                    data = new
                    {
                        id = cost.Id.ToString(),
                        status = cost.Status,
                        paidAt = cost.PaidAt,
                        updatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weekly cost");
                throw;
            }
        }

        // DELETE /costs/weekly/:id (Manager)
        [HttpDelete("weekly/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<ActionResult<object>> DeleteWeeklyCost(int id)
        {
            try
            {
                var managerUserId = GetUserId();
                var cost = await _context.WeeklyCosts
                    .Include(c => c.Employee)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cost == null)
                {
                    throw new NotFoundException("التكلفة غير موجودة");
                }

                // Verify employee belongs to manager
                if (cost.Employee.ManagerUserId != managerUserId)
                {
                    throw new UnauthorizedException("غير مصرح لك بحذف هذه التكلفة");
                }

                _context.WeeklyCosts.Remove(cost);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم حذف التكلفة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weekly cost");
                throw;
            }
        }

        // GET /costs/employees/:employeeId/summary (Manager)
        [HttpGet("employees/{employeeId}/summary")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetEmployeeCostsSummary(
            string employeeId,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            try
            {
                var managerUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(employeeId);
                
                if (employee == null || employee.ManagerUserId != managerUserId)
                {
                    throw new UnauthorizedException("غير مصرح لك بالوصول إلى بيانات هذا الموظف");
                }

                var currentDate = DateTime.UtcNow;
                var targetMonth = month ?? currentDate.Month;
                var targetYear = year ?? currentDate.Year;

                var costs = await _context.WeeklyCosts
                    .Where(c => c.EmployeeId == employeeId &&
                               c.Month == targetMonth &&
                               c.Year == targetYear)
                    .ToListAsync();

                var total = costs.Sum(c => (double)c.Amount);
                var paid = costs.Where(c => c.Status == "paid").Sum(c => (double)c.Amount);
                var pending = costs.Where(c => c.Status == "pending").Sum(c => (double)c.Amount);

                var costsList = costs.Select(c => new
                {
                    id = c.Id.ToString(),
                    weekNumber = c.WeekNumber,
                    description = c.Description,
                    amount = c.Amount,
                    status = c.Status
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        employeeId = employeeId,
                        employeeName = employee.User.FullName,
                        month = targetMonth,
                        year = targetYear,
                        total = total,
                        paid = paid,
                        pending = pending,
                        costs = costsList
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee costs summary");
                throw;
            }
        }
    }

    public class CreateWeeklyCostDto
    {
        public string EmployeeId { get; set; } = null!;
        public int WeekNumber { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public string CostType { get; set; } = "expense";
        public string? Status { get; set; }
    }

    public class UpdateWeeklyCostDto
    {
        public string? Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}

