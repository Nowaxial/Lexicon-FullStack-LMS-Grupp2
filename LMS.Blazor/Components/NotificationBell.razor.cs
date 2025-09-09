using LMS.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LMS.Blazor.Components
{
    public partial class NotificationBell
    {
        private List<NotificationItem>? notifications;
        private NotificationItem? selectedNotification;
        private ContactMessageDto? decryptedMessage;
        private string? errorMessage;
        private bool showNotificationModal = false;
        private int unreadCount => notifications?.Count(n => !n.IsRead) ?? 0;
        private Timer? _timer;
        private bool isTeacher = false;
        private string? userId;

        private string GetStudentName(string message)
        {
            var parts = message.Split(" laddade upp '");
            return parts[0];
        }

        private string GetFileName(string message)
        {
            var parts = message.Split(" laddade upp '");
            if (parts.Length > 1)
            {
                var fileParts = parts[1].Split("' i ");
                return fileParts[0];
            }
            return "";
        }

        private string GetCoursePath(string message)
        {
            var parts = message.Split(" laddade upp '");
            if (parts.Length > 1)
            {
                var fileParts = parts[1].Split("' i ");
                if (fileParts.Length > 1)
                {
                    return fileParts[1].Split('|')[0];
                }
            }
            return "";
        }

        private int GetDocumentId(string message)
        {
            var parts = message.Split('|');
            if (parts.Length > 1 && int.TryParse(parts[1], out int documentId))
            {
                return documentId;
            }
            return 0;
        }

        private void OpenNotificationModal()
        {
            showNotificationModal = true;
        }

        private void CloseNotificationModal()
        {
            showNotificationModal = false;
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            isTeacher = authState.User.IsInRole("Teacher");
            userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (isTeacher && !string.IsNullOrEmpty(userId))
            {
                await LoadNotifications();
                _timer = new Timer(async _ =>
                {
                    try
                    {
                        await InvokeAsync(LoadNotifications);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Timer error: {ex.Message}");
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            }
        }

        private async Task LoadNotifications()
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return;

                notifications = await NotificationService.GetNotificationsForUserAsync(userId);
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadNotifications ERROR: {ex.Message}");
            }
        }

        private async Task ShowNotification(NotificationItem notification)
        {
            showNotificationModal = false;
            selectedNotification = notification;
            decryptedMessage = null;
            errorMessage = null;

            if (notification.Message.Contains("kontaktmeddelande från"))
            {
                try
                {
                    var parts = notification.Message.Split(": ");
                    if (parts.Length >= 2)
                    {
                        var subject = parts[1];
                        var contactMessages = await NotificationService.GetAllContactMessagesAsync();
                        var matchingMessage = contactMessages
                            .Where(m => m.Subject == subject)
                            .OrderByDescending(m => m.Timestamp)
                            .FirstOrDefault();

                        if (matchingMessage != null)
                        {
                            decryptedMessage = await NotificationService.DecryptMessageAsync(matchingMessage.Id);
                        }
                        else
                        {
                            errorMessage = $"Kunde inte hitta kontaktmeddelande med ämne: {subject}";
                        }
                    }
                    else
                    {
                        errorMessage = "Kunde inte extrahera ämne från notifikationen";
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Fel vid dekryptering: {ex.Message}";
                }
            }
            else if (notification.Message.Contains("laddade upp"))
            {
                StateHasChanged();
                return;
            }
            else
            {
                errorMessage = "Okänd notifikationstyp";
            }

            StateHasChanged();
        }

        private async Task DownloadFile(int documentId)
        {
            await JSRuntime.InvokeVoidAsync("eval", $"window.location.href = '/proxy/download-document/{documentId}'");
        }

        private async Task DeleteNotification(string id)
        {
            try
            {
                await NotificationService.DeleteNotificationAsync(id);
                await LoadNotifications();
                CloseModal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteNotification error: {ex.Message}");
            }
        }

        private void CloseModal()
        {
            selectedNotification = null;
            decryptedMessage = null;
            errorMessage = null;
            StateHasChanged();
        }

        private async Task MarkAsRead(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return;

                await NotificationService.MarkAsReadAsync(id, userId);
                await LoadNotifications();
                CloseModal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkAsRead error: {ex.Message}");
            }
        }

        private async Task MarkAsUnread(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return;

                await NotificationService.MarkAsUnreadAsync(id, userId);
                await LoadNotifications();
                CloseModal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkAsUnread error: {ex.Message}");
            }
        }

        public void Dispose() => _timer?.Dispose();
    }
}