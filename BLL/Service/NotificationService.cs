using BLL.ServiceAbstraction;
using DAL.Data;
using DAL.Data.Models.NotificationsLogs;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(
            string userId,
            string type,
            string title,
            string message,
            string priority = "medium",
            string? link = null,
            string? relatedId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    Priority = priority,
                    Link = link,
                    RelatedId = relatedId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Notification created: Type={Type}, UserId={UserId}, Title={Title}",
                    type, userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
            }
        }

        public async Task CreateBulkNotificationsAsync(
            List<string> userIds,
            string type,
            string title,
            string message,
            string priority = "medium",
            string? link = null,
            string? relatedId = null)
        {
            try
            {
                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    Priority = priority,
                    Link = link,
                    RelatedId = relatedId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Bulk notifications created: Type={Type}, Count={Count}",
                    type, userIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk notifications");
            }
        }
    }
}