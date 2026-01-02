using DAL.Data.Models.NotificationsLogs;

namespace BLL.ServiceAbstraction
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(
            string userId,
            string type,
            string title,
            string message,
            string priority = "medium",
            string? link = null,
            string? relatedId = null);

        Task CreateBulkNotificationsAsync(
            List<string> userIds,
            string type,
            string title,
            string message,
            string priority = "medium",
            string? link = null,
            string? relatedId = null);
    }
}