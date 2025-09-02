using LMS.Shared.DTOs;

namespace Service.Contracts;

public interface INotificationService
{
    Task SaveContactMessageAsync(ContactMessageDto message);
    Task<List<NotificationItem>> GetNotificationsForUserAsync(string userId);
    Task MarkAsReadAsync(string notificationId, string userId);
    Task<List<ContactMessageItem>> GetAllContactMessagesAsync();
    Task<ContactMessageDto?> DecryptMessageAsync(Guid id);
    Task MarkAsUnreadAsync(string notificationId, string userId);
    Task<ContactMessageDto?> DecryptContactMessageDirectAsync(string encryptedData);


}
