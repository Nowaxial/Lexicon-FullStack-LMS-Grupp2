using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Service.Contracts;

namespace LMS.Services
{
    public class NotificationService : INotificationService
    {
        private readonly BlobNotificationRepository _blobRepo;
        private readonly EncryptionService _encryptionService;
        private const string NotificationsFileName = "notifications.json";
        private const string ContactMessagesFileName = "contact-messages.json";

        private class StoredContactMessage
        {
            public Guid Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;

            // Stödjer både gammalt och nytt format
            public string? Message { get; set; } // Nytt format
            public string? EncryptedData { get; set; } // Gammalt format
        }

        public NotificationService(IConfiguration config, EncryptionService encryptionService)
        {
            var connectionString = config["AzureBlob:ConnectionString"];
            var containerName = config["AzureBlob:ContainerName"];
            _blobRepo = new BlobNotificationRepository(connectionString, containerName);
            _encryptionService = encryptionService;

            Console.WriteLine("[NotificationService] Initierad med Azure Blob Storage och EncryptionService.");
        }

        private async Task<List<NotificationItem>> LoadAllNotificationsAsync()
        {
            var json = await _blobRepo.LoadAsync(NotificationsFileName);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("[NotificationService] Inga notifikationer funna i blob.");
                return new List<NotificationItem>();
            }

            var notifications = JsonSerializer.Deserialize<List<NotificationItem>>(json) ?? new List<NotificationItem>();
            Console.WriteLine($"[NotificationService] Laddade {notifications.Count} notifikationer från blob.");
            return notifications;
        }

        private async Task SaveNotificationsAsync(List<NotificationItem> notifications)
        {
            var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions { WriteIndented = true });
            await _blobRepo.SaveAsync(NotificationsFileName, json);
            Console.WriteLine($"[NotificationService] Sparade {notifications.Count} notifikationer till blob.");
        }

        private async Task<List<StoredContactMessage>> LoadAllStoredContactMessagesAsync()
        {
            var json = await _blobRepo.LoadAsync(ContactMessagesFileName);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("[NotificationService] Inga kontaktmeddelanden funna i blob.");
                return new List<StoredContactMessage>();
            }

            var messages = JsonSerializer.Deserialize<List<StoredContactMessage>>(json) ?? new List<StoredContactMessage>();
            Console.WriteLine($"[NotificationService] Laddade {messages.Count} kontaktmeddelanden från blob.");
            return messages;
        }

        private async Task SaveStoredContactMessagesAsync(List<StoredContactMessage> messages)
        {
            var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            await _blobRepo.SaveAsync(ContactMessagesFileName, json);
            Console.WriteLine($"[NotificationService] Sparade {messages.Count} kontaktmeddelanden till blob.");
        }

        public async Task SaveContactMessageAsync(ContactMessageDto message)
        {
            var storedMessages = await LoadAllStoredContactMessagesAsync();

            var serializedMessage = JsonSerializer.Serialize(message);
            var encryptedMessage = _encryptionService.Encrypt(serializedMessage);

            var storedMessage = new StoredContactMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Name = message.Name,
                Email = message.Email,
                Subject = message.Subject,
                Message = encryptedMessage
            };

            storedMessages.Insert(0, storedMessage);
            await SaveStoredContactMessagesAsync(storedMessages);

            // På exakt detta format beroende på frontendmatchning
            var notificationMessage = $"kontaktmeddelande från {storedMessage.Name}: {storedMessage.Subject}|{storedMessage.Id}";

            Console.WriteLine($"[NotificationService] Skapar notifikation med meddelande: '{notificationMessage}'");

            var notifications = await LoadAllNotificationsAsync();
            var newNotification = new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                Message = notificationMessage,
                Timestamp = storedMessage.Timestamp,
                ReadBy = new List<string>(),
                TargetTeachers = new List<string>()
            };
            notifications.Insert(0, newNotification);
            await SaveNotificationsAsync(notifications);

            Console.WriteLine($"[NotificationService] Ny notifikation skapad med ID: {newNotification.Id} och meddelande: '{newNotification.Message}'");
        }


        public async Task<List<ContactMessageItem>> GetAllContactMessagesAsync()
        {
            var storedMessages = await LoadAllStoredContactMessagesAsync();
            return storedMessages.Select(m => new ContactMessageItem
            {
                Id = m.Id,
                Timestamp = m.Timestamp,
                Subject = m.Subject
            }).ToList();
        }

        public async Task<ContactMessageDto?> DecryptMessageAsync(Guid id)
        {
            var storedMessages = await LoadAllStoredContactMessagesAsync();
            var storedMessage = storedMessages.FirstOrDefault(m => m.Id == id);
            if (storedMessage == null) return null;

            var encryptedData = storedMessage.EncryptedData ?? storedMessage.Message;
            if (string.IsNullOrEmpty(encryptedData)) return null;

            try
            {
                var decryptedJson = _encryptionService.Decrypt(encryptedData);
                return JsonSerializer.Deserialize<ContactMessageDto>(decryptedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Fel vid dekryptering av meddelande: {ex.Message}");
                return null;
            }
        }

        public async Task<List<NotificationItem>> GetNotificationsForUserAsync(string userId)
        {
            Console.WriteLine($"[NotificationService] Hämtar notifikationer för användare {userId}...");
            var notifications = await LoadAllNotificationsAsync();
            Console.WriteLine($"[NotificationService] Totalt {notifications.Count} notifikationer laddade från blob.");
            
            foreach (var n in notifications)
            {
                Console.WriteLine($"[NotificationService] Notifikation: ID={n.Id}, Message='{n.Message}', TargetTeachers.Count={n.TargetTeachers.Count}");
            }

            var filtered = notifications.Where(n => n.TargetTeachers.Count == 0 || n.TargetTeachers.Contains(userId))
                .Select(n => new NotificationItem
                {
                    Id = n.Id,
                    Message = n.Message,
                    Timestamp = n.Timestamp,
                    IsRead = n.ReadBy.Contains(userId),
                    ReadBy = n.ReadBy,
                    TargetTeachers = n.TargetTeachers
                }).ToList();

            Console.WriteLine($"[NotificationService] Returnerar {filtered.Count} notifikationer för användare {userId}.");
            return filtered;
        }

        public async Task MarkAsReadAsync(string notificationId, string userId)
        {
            var notifications = await LoadAllNotificationsAsync();
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);

            if (notification != null && !notification.ReadBy.Contains(userId))
            {
                notification.ReadBy.Add(userId);
                await SaveNotificationsAsync(notifications);
                Console.WriteLine($"[NotificationService] Notifikation {notificationId} markerad som läst av {userId}.");
            }
        }

        public async Task MarkAsUnreadAsync(string notificationId, string userId)
        {
            var notifications = await LoadAllNotificationsAsync();
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);

            if (notification != null && notification.ReadBy.Contains(userId))
            {
                notification.ReadBy.Remove(userId);
                await SaveNotificationsAsync(notifications);
                Console.WriteLine($"[NotificationService] Notifikation {notificationId} markerad som oläst av {userId}.");
            }
        }

        public async Task DeleteNotificationAsync(string notificationId)
        {
            var notifications = await LoadAllNotificationsAsync();
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);

            if (notification != null)
            {
                notifications.Remove(notification);
                await SaveNotificationsAsync(notifications);
                Console.WriteLine($"[NotificationService] Notifikation {notificationId} borttagen.");
            }
        }

        public async Task DeleteNotificationByDocumentIdAsync(int documentId)
        {
            var notifications = await LoadAllNotificationsAsync();
            var toRemove = notifications.Where(n => n.Message.Contains($"|{documentId}")).ToList();

            foreach (var notification in toRemove)
            {
                notifications.Remove(notification);
            }

            if (toRemove.Any())
            {
                await SaveNotificationsAsync(notifications);
                Console.WriteLine($"[NotificationService] Notifikationer relaterade till dokument {documentId} borttagna.");
            }
        }

        public async Task NotifyFileUploadAsync(string studentName, string courseName, string moduleName, string activityTitle, string fileName, int documentId, int courseId)
        {
            var message = $"{studentName} laddade upp '{fileName}' i {courseName} > {moduleName} > {activityTitle}|{documentId}";
            var newNotification = new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                Message = message,
                Timestamp = DateTime.Now,
                ReadBy = new List<string>(),
                TargetTeachers = new List<string>()
            };

            var notifications = await LoadAllNotificationsAsync();
            notifications.Insert(0, newNotification);
            await SaveNotificationsAsync(notifications);

            Console.WriteLine($"[NotificationService] Filuppladdningsnotifikation skapad: {message}");
        }

        public async Task<ContactMessageDto?> DecryptContactMessageDirectAsync(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                return null;

            try
            {
                var decryptedJson = _encryptionService.Decrypt(encryptedData);
                var contactMessage = JsonSerializer.Deserialize<ContactMessageDto>(decryptedJson);
                return await Task.FromResult(contactMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Fel vid direkt dekryptering: {ex.Message}");
                return null;
            }
        }
    }
}
