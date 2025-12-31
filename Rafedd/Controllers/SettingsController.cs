using DAL.Data;
using DAL.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ApplicationDbContext context,
            ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // GET /settings
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetSettings()
        {
            try
            {
                var userId = GetUserId();
                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    // Create default settings
                    settings = new UserSettings
                    {
                        UserId = userId,
                        Language = "ar",
                        EmailNotifications = true,
                        PushNotifications = true,
                        SmsNotifications = false,
                        Theme = "light",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        language = settings.Language,
                        notifications = new
                        {
                            email = settings.EmailNotifications,
                            push = settings.PushNotifications,
                            sms = settings.SmsNotifications
                        },
                        theme = settings.Theme
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings");
                throw;
            }
        }

        // PUT /settings
        [HttpPut]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<ActionResult<object>> UpdateSettings([FromBody] UpdateSettingsDto dto)
        {
            try
            {
                var userId = GetUserId();
                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    settings = new UserSettings
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserSettings.Add(settings);
                }

                if (!string.IsNullOrEmpty(dto.Language))
                {
                    settings.Language = dto.Language;
                }

                if (dto.Notifications != null)
                {
                    if (dto.Notifications.Email.HasValue)
                    {
                        settings.EmailNotifications = dto.Notifications.Email.Value;
                    }
                    if (dto.Notifications.Push.HasValue)
                    {
                        settings.PushNotifications = dto.Notifications.Push.Value;
                    }
                    if (dto.Notifications.Sms.HasValue)
                    {
                        settings.SmsNotifications = dto.Notifications.Sms.Value;
                    }
                }

                if (!string.IsNullOrEmpty(dto.Theme))
                {
                    settings.Theme = dto.Theme;
                }

                settings.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث الإعدادات بنجاح",
                    data = new
                    {
                        language = settings.Language,
                        notifications = new
                        {
                            email = settings.EmailNotifications,
                            push = settings.PushNotifications,
                            sms = settings.SmsNotifications
                        },
                        theme = settings.Theme,
                        updatedAt = settings.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings");
                throw;
            }
        }
    }

    public class UpdateSettingsDto
    {
        public string? Language { get; set; }
        public NotificationSettingsDto? Notifications { get; set; }
        public string? Theme { get; set; }
    }

    public class NotificationSettingsDto
    {
        public bool? Email { get; set; }
        public bool? Push { get; set; }
        public bool? Sms { get; set; }
    }
}

