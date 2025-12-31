using DAL.Data;
using DAL.Data.Models;
using DAL.Data.Models.IdentityModels;
using DAL.Repositories.RepositoryIntrfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using System.Security.Claims;
using System.Text.Json;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/suggestions")]
    [Authorize]
    public class SuggestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<SuggestionsController> _logger;

        public SuggestionsController(
            ApplicationDbContext context,
            IEmployeeRepository employeeRepository,
            ILogger<SuggestionsController> logger)
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

        // GET /suggestions (Manager)
        [HttpGet]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetSuggestions(
            [FromQuery] string? status = null,
            [FromQuery] string? employeeId = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            try
            {
                var managerUserId = GetUserId();
                var employees = await _employeeRepository.GetByManagerAsync(managerUserId);
                var employeeIds = employees.Select(e => e.UserId).ToList();

                var query = _context.Suggestions
                    .Include(s => s.Employee)
                        .ThenInclude(e => e.User)
                    .Where(s => employeeIds.Contains(s.EmployeeId));

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.Status == status);
                }

                if (!string.IsNullOrEmpty(employeeId))
                {
                    if (employeeIds.Contains(employeeId))
                    {
                        query = query.Where(s => s.EmployeeId == employeeId);
                    }
                    else
                    {
                        throw new UnauthorizedException("غير مصرح لك بالوصول إلى مقترحات هذا الموظف");
                    }
                }

                var total = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(total / (double)limit);
                var skip = (page - 1) * limit;

                var suggestionsData = await query
                    .Skip(skip)
                    .Take(limit)
                    .Select(s => new
                    {
                        id = s.Id.ToString(),
                        employeeId = s.EmployeeId,
                        employeeName = s.Employee.User.FullName,
                        title = s.Title,
                        details = s.Details,
                        status = s.Status,
                        attachments = s.Attachments,
                        createdAt = s.CreatedAt
                    })
                    .ToListAsync();

                var suggestions = suggestionsData.Select(s => new
                {
                    s.id,
                    s.employeeId,
                    s.employeeName,
                    s.title,
                    s.details,
                    s.status,
                    attachments = string.IsNullOrEmpty(s.attachments)
                        ? Array.Empty<string>()
                        : JsonSerializer.Deserialize<string[]>(s.attachments) ?? Array.Empty<string>(),
                    s.createdAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = suggestions,
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
                _logger.LogError(ex, "Error getting suggestions");
                throw;
            }
        }

        // GET /suggestions/me (Employee)
        [HttpGet("me")]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetMySuggestions()
        {
            try
            {
                var employeeUserId = GetUserId();

                var suggestionsData = await _context.Suggestions
                    .Include(s => s.Employee)
                        .ThenInclude(e => e.User)
                    .Where(s => s.EmployeeId == employeeUserId)
                    .Select(s => new
                    {
                        id = s.Id.ToString(),
                        employeeId = s.EmployeeId,
                        employeeName = s.Employee.User.FullName,
                        title = s.Title,
                        details = s.Details,
                        status = s.Status,
                        attachments = s.Attachments,
                        createdAt = s.CreatedAt
                    })
                    .ToListAsync();

                var suggestions = suggestionsData.Select(s => new
                {
                    s.id,
                    s.employeeId,
                    s.employeeName,
                    s.title,
                    s.details,
                    s.status,
                    attachments = string.IsNullOrEmpty(s.attachments)
                        ? Array.Empty<string>()
                        : JsonSerializer.Deserialize<string[]>(s.attachments) ?? Array.Empty<string>(),
                    s.createdAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = suggestions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee suggestions");
                throw;
            }
        }

        // POST /suggestions (Employee)
        [HttpPost]
        [Authorize(Roles = "Employee")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<ActionResult<object>> CreateSuggestion([FromBody] CreateSuggestionDto dto)
        {
            try
            {
                var employeeUserId = GetUserId();
                var employee = await _employeeRepository.GetByUserIdAsync(employeeUserId);
                
                if (employee == null)
                {
                    throw new NotFoundException("الموظف غير موجود");
                }

                var suggestion = new Suggestion
                {
                    EmployeeId = employeeUserId,
                    Title = dto.Title,
                    Details = dto.Details,
                    Status = "pending",
                    Attachments = dto.Attachments != null && dto.Attachments.Length > 0
                        ? JsonSerializer.Serialize(dto.Attachments)
                        : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Suggestions.Add(suggestion);
                await _context.SaveChangesAsync();

                var suggestionDto = new
                {
                    id = suggestion.Id.ToString(),
                    employeeId = suggestion.EmployeeId,
                    employeeName = employee.User.FullName,
                    title = suggestion.Title,
                    details = suggestion.Details,
                    status = suggestion.Status,
                    attachments = dto.Attachments ?? Array.Empty<string>(),
                    createdAt = suggestion.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "تم إرسال المقترح بنجاح",
                    data = suggestionDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating suggestion");
                throw;
            }
        }

        // PUT /suggestions/:id/review (Manager)
        [HttpPut("{id}/review")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<ActionResult<object>> ReviewSuggestion(int id, [FromBody] ReviewSuggestionDto dto)
        {
            try
            {
                var managerUserId = GetUserId();
                var suggestion = await _context.Suggestions
                    .Include(s => s.Employee)
                        .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (suggestion == null)
                {
                    throw new NotFoundException("المقترح غير موجود");
                }

                // Verify employee belongs to manager
                if (suggestion.Employee.ManagerUserId != managerUserId)
                {
                    throw new UnauthorizedException("غير مصرح لك بمراجعة هذا المقترح");
                }

                suggestion.Status = dto.Status;
                suggestion.ReviewNotes = dto.ReviewNotes;
                suggestion.ReviewedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم مراجعة المقترح بنجاح",
                    data = new
                    {
                        id = suggestion.Id.ToString(),
                        status = suggestion.Status,
                        reviewNotes = suggestion.ReviewNotes,
                        reviewedAt = suggestion.ReviewedAt,
                        updatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing suggestion");
                throw;
            }
        }
    }

    public class CreateSuggestionDto
    {
        public string Title { get; set; } = null!;
        public string Details { get; set; } = null!;
        public string[]? Attachments { get; set; }
    }

    public class ReviewSuggestionDto
    {
        public string Status { get; set; } = null!; // approved, rejected
        public string? ReviewNotes { get; set; }
    }
}

