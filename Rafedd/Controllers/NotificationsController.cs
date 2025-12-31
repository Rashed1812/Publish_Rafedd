using DAL.Data;
using DAL.Data.Models.NotificationsLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedException("User ID not found in token");
        }

        // GET /notifications
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> GetNotifications(
            [FromQuery] bool? read = null,
            [FromQuery] string? type = null,
            [FromQuery] string? priority = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetUserId();

                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (read.HasValue)
                {
                    query = query.Where(n => n.IsRead == read.Value);
                }

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(n => n.Type == type);
                }

                if (!string.IsNullOrEmpty(priority))
                {
                    query = query.Where(n => n.Priority == priority);
                }

                var total = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(total / (double)limit);
                var skip = (page - 1) * limit;

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip(skip)
                    .Take(limit)
                    .Select(n => new
                    {
                        id = n.Id.ToString(),
                        userId = n.UserId,
                        type = n.Type,
                        title = n.Title,
                        message = n.Message,
                        priority = n.Priority,
                        read = n.IsRead,
                        link = n.Link,
                        relatedId = n.RelatedId,
                        createdAt = n.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifications,
                    pagination = new
                    {
                        page,
                        limit,
                        total,
                        totalPages
                    },
                    unreadCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                throw;
            }
        }

        // PUT /notifications/:id/read
        [HttpPut("{id}/read")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<ActionResult<object>> MarkNotificationAsRead(int id)
        {
            try
            {
                var userId = GetUserId();
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    throw new NotFoundException("الإشعار غير موجود");
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث حالة الإشعار",
                    data = new
                    {
                        id = notification.Id.ToString(),
                        read = notification.IsRead,
                        updatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                throw;
            }
        }

        // PUT /notifications/read-all
        [HttpPut("read-all")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث جميع الإشعارات",
                    count = notifications.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                throw;
            }
        }

        // DELETE /notifications/:id
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<ActionResult<object>> DeleteNotification(int id)
        {
            try
            {
                var userId = GetUserId();
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    throw new NotFoundException("الإشعار غير موجود");
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم حذف الإشعار بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                throw;
            }
        }
    }
}

