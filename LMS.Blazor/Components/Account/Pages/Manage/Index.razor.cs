using Domain.Models.Entities;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace LMS.Blazor.Components.Account.Pages.Manage
{
    public partial class Index
    {
        private ApplicationUser user = default!;
        private string? username;
        private string? phoneNumber;
        private bool isLoading = true;

        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default!;

        [SupplyParameterFromForm]
        private InputModel Input { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            user = await UserAccessor.GetRequiredUserAsync(HttpContext);
            username = await UserManager.GetUserNameAsync(user);
            phoneNumber = await UserManager.GetPhoneNumberAsync(user);

            Input.PhoneNumber ??= phoneNumber;
            Input.FirstName ??= user.FirstName;
            Input.LastName ??= user.LastName;

            isLoading = false;
        }

        private async Task OnValidSubmitAsync()
        {
            var hasChanges = false;

            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await UserManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    RedirectManager.RedirectToCurrentPageWithStatus("Error: Failed to set phone number.", HttpContext);
                    return;
                }
                hasChanges = true;
            }

            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                hasChanges = true;
            }

            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                hasChanges = true;
            }

            if (hasChanges)
            {
                var updateResult = await UserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    RedirectManager.RedirectToCurrentPageWithStatus("Error: Failed to update profile.", HttpContext);
                    return;
                }
            }

            await SignInManager.RefreshSignInAsync(user);
            RedirectManager.RedirectToCurrentPageWithStatus("Din profil har uppdaterats", HttpContext);
        }

        private sealed class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "First name")]
            public string? FirstName { get; set; }

            [Display(Name = "Last name")]
            public string? LastName { get; set; }
        }
    }
}
