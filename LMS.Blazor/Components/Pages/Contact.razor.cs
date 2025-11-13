using LMS.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace LMS.Blazor.Components.Pages
{
    public partial class Contact
    {
        [SupplyParameterFromForm]
        private ContactFormModel Input { get; set; } = new();

        private string message = string.Empty;
        private bool isSuccess = false;
        private bool isSubmitting = false;

        private async Task SendMessage()
        {
            try
            {
                isSubmitting = true;

                var dto = new ContactMessageDto
                {
                    Name = Input.Name,
                    Email = Input.Email,
                    Subject = Input.Subject,
                    Message = Input.Message
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiBaseUrl = Configuration["LmsAPIBaseAddress"];

                // ÄNDRAT: Uppdaterad endpoint till /api/notifications/contact
                var response = await Http.PostAsync($"{apiBaseUrl}/api/notifications/contact", content);

                response.EnsureSuccessStatusCode();

                Input = new ContactFormModel();
                message = "Ditt meddelande har skickats! Vi återkommer så snart som möjligt.";
                isSuccess = true;
            }
            catch (Exception ex)
            {
                message = $"Fel: {ex.Message}";
                isSuccess = false;
                Console.WriteLine($"[ContactPage] Error: {ex.Message}");
                Console.WriteLine($"[ContactPage] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                isSubmitting = false;
            }
        }

        public class ContactFormModel
        {
            [Required(ErrorMessage = "Namn är obligatoriskt")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "E-post är obligatoriskt")]
            [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ämne är obligatoriskt")]
            public string Subject { get; set; } = string.Empty;

            [Required(ErrorMessage = "Meddelande är obligatoriskt")]
            [MinLength(10, ErrorMessage = "Meddelandet måste vara minst 10 tecken")]
            public string Message { get; set; } = string.Empty;
        }
    }
}