using Domain.Models.Entities;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace LMS.Blazor.Components.Account.Pages.Manage
{
    public partial class DeletePersonalData
    {
        private string? message;
        private ApplicationUser user = default!;
        private bool requirePassword;

        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default!;

        [SupplyParameterFromForm]
        private InputModel Input { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            Input ??= new();
            user = await UserAccessor.GetRequiredUserAsync(HttpContext);
            requirePassword = await UserManager.HasPasswordAsync(user);
        }

        private async Task OnValidSubmitAsync()
        {
            if (requirePassword && !await UserManager.CheckPasswordAsync(user, Input.Password))
            {
                message = "Fel: Felaktigt lösenord.";
                return;
            }

            var result = await UserManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Unexpected error occurred deleting user.");
            }

            await SignInManager.SignOutAsync();

            var userId = await UserManager.GetUserIdAsync(user);
            Logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            RedirectManager.RedirectTo("/");

        }

        private sealed class InputModel
        {
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";
        }
    }
}