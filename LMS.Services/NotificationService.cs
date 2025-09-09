using System.Text.Json;
using LMS.Shared.DTOs;
using Service.Contracts;

namespace LMS.Services;

public class NotificationService : INotificationService
{
    private static readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LMS");
    private static readonly string NotificationsFilePath = Path.Combine(DataFolder, "notifications.json");
    private static readonly string ContactFilePath = Path.Combine(DataFolder, "contact-messages.json");
    private readonly EncryptionService _encryptionService;

    public NotificationService(EncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
        Directory.CreateDirectory(DataFolder);
    }

    public async Task SaveContactMessageAsync(ContactMessageDto message)
    {
        await SaveContactToFileAsync(message);
        await AddNotificationAsync($"Nytt kontaktmeddelande från {message.Name}: {message.Subject}");
    }

    public async Task<List<NotificationItem>> GetNotificationsForUserAsync(string userId)
    {
        var notifications = await GetAllNotificationsAsync();
        return notifications.Select(n => new NotificationItem
        {
            Id = n.Id,
            Message = n.Message,
            Timestamp = n.Timestamp,
            IsRead = n.ReadBy.Contains(userId),
            ReadBy = n.ReadBy
        }).ToList();
    }

    public async Task MarkAsReadAsync(string notificationId, string userId)
    {
        var notifications = await GetAllNotificationsAsync();
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);

        if (notification != null && !notification.ReadBy.Contains(userId))
        {
            notification.ReadBy.Add(userId);
            await SaveNotificationsAsync(notifications);
        }
    }

    public async Task<List<ContactMessageItem>> GetAllContactMessagesAsync()
    {
        if (!File.Exists(ContactFilePath))
            return new List<ContactMessageItem>();

        var json = await File.ReadAllTextAsync(ContactFilePath);
        var messages = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();

        return messages.Select(m => new ContactMessageItem
        {
            Id = m.GetProperty("Id").GetGuid(),
            Timestamp = m.GetProperty("Timestamp").GetDateTime(),
            Subject = m.GetProperty("Subject").GetString() ?? ""
        }).ToList();
    }

    public async Task<ContactMessageDto?> DecryptMessageAsync(Guid id)
    {
        if (!File.Exists(ContactFilePath))
            return null;

        var json = await File.ReadAllTextAsync(ContactFilePath);
        var messages = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();

        var message = messages.FirstOrDefault(m => m.GetProperty("Id").GetGuid() == id);

        if (message.ValueKind == JsonValueKind.Undefined)
            return null;

        return _encryptionService.DecryptObject<ContactMessageDto>(
            message.GetProperty("EncryptedData").GetString()!);
    }

    private async Task SaveContactToFileAsync(ContactMessageDto message)
    {
        var encryptedMessage = _encryptionService.EncryptObject(message);

        var contactMessage = new
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.Now,
            Subject = message.Subject,
            EncryptedData = encryptedMessage
        };

        var messages = new List<object>();

        if (File.Exists(ContactFilePath))
        {
            var json = await File.ReadAllTextAsync(ContactFilePath);
            if (!string.IsNullOrEmpty(json))
                messages = JsonSerializer.Deserialize<List<object>>(json) ?? new();
        }

        messages.Insert(0, contactMessage);

        var newJson = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ContactFilePath, newJson);
    }

    private async Task AddNotificationAsync(string message)
    {
        var notifications = await GetAllNotificationsAsync();

        var newNotification = new NotificationItem
        {
            Id = Guid.NewGuid().ToString(),
            Message = message,
            Timestamp = DateTime.Now,
            ReadBy = new List<string>()
        };

        notifications.Insert(0, newNotification);
        await SaveNotificationsAsync(notifications);
    }

    private async Task<List<NotificationItem>> GetAllNotificationsAsync()
    {
        if (!File.Exists(NotificationsFilePath)) return new();

        var json = await File.ReadAllTextAsync(NotificationsFilePath);
        return JsonSerializer.Deserialize<List<NotificationItem>>(json) ?? new();
    }

    private async Task SaveNotificationsAsync(List<NotificationItem> notifications)
    {
        var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(NotificationsFilePath, json);
    }

    public async Task MarkAsUnreadAsync(string notificationId, string userId)
    {
        var notifications = await GetAllNotificationsAsync();
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);

        if (notification != null && notification.ReadBy.Contains(userId))
        {
            notification.ReadBy.Remove(userId);
            await SaveNotificationsAsync(notifications);
        }
    }
    public async Task<ContactMessageDto?> DecryptContactMessageDirectAsync(string encryptedData)
    {
        try
        {
            return _encryptionService.DecryptObject<ContactMessageDto>(encryptedData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decrypting contact message: {ex.Message}");
            return null;
        }
    }

    public async Task DeleteNotificationAsync(string notificationId)
    {
        var notifications = await GetAllNotificationsAsync();
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);

        if (notification != null)
        {
            notifications.Remove(notification);
            await SaveNotificationsAsync(notifications);
        }
    }

    public async Task NotifyFileUploadAsync(string studentName, string courseName, string moduleName, string activityTitle, string fileName, int documentId)
    {
        var message = $"{studentName} laddade upp '{fileName}' i {courseName} > {moduleName} > {activityTitle}|{documentId}";
        await AddNotificationAsync(message);
    }

    public async Task DeleteNotificationByDocumentIdAsync(int documentId)
    {
        var notifications = await GetAllNotificationsAsync();
        var notificationsToRemove = notifications.Where(n =>
            n.Message.Contains($"|{documentId}")).ToList();

        foreach (var notification in notificationsToRemove)
        {
            notifications.Remove(notification);
        }

        if (notificationsToRemove.Any())
        {
            await SaveNotificationsAsync(notifications);
        }
    }


}
