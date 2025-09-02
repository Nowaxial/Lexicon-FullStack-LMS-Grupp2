using Domain.Models.Entities;
using LMS.Blazor.Services;
using LMS.Shared.DTOs.AuthDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LMS.Blazor.Components.Account.Pages
{

    public partial class Login
    {
        private string? errorMessage;

        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default!;

        [SupplyParameterFromForm]
        private InputModel Input { get; set; } = new();

        [SupplyParameterFromQuery]
        private string? ReturnUrl { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (HttpMethods.IsGet(HttpContext.Request.Method))
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task LoginUser()
        {
            var user = await UserManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                errorMessage = "E-postadressen eller lösenordet är felaktigt.";
                return;
            }

            var tokens = await GetTokensFromApi(user.UserName!, Input.Password);
            if (tokens == null)
            {
                errorMessage = "Inloggning misslyckades, försök igen.";
                return;
            }

            var result = await SignInManager.PasswordSignInAsync(
                user, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await TokenStorage.StoreTokensAsync(user.Id, tokens);
                Logger.LogInformation("User logged in.");

                var roles = await UserManager.GetRolesAsync(user);
                var target = ResolveTargetUrl(roles, ReturnUrl);

                NavigationManager.NavigateTo(target, forceLoad: true, replace: true);
                return;
            }
            else if (result.RequiresTwoFactor)
            {
                NavigationManager.NavigateTo(
                    $"Account/LoginWith2fa?returnUrl={Uri.EscapeDataString(ReturnUrl ?? "/")}&rememberMe={Input.RememberMe}",
                    forceLoad: true, replace: true);
                return;
            }
            else if (result.IsLockedOut)
            {
                NavigationManager.NavigateTo("Account/Lockout", forceLoad: true, replace: true);
                return;
            }
            errorMessage = "Felaktiga inloggningsuppgifter.";
        }

        private string ResolveTargetUrl(IList<string> roles, string? returnUrl)
        {
            if (IsLocalUrl(returnUrl))
                return returnUrl!;

            if (roles.Any(r => string.Equals(r, "Teacher", StringComparison.OrdinalIgnoreCase)))
                return "/teacher";

            if (roles.Any(r => string.Equals(r, "Student", StringComparison.OrdinalIgnoreCase)))
                return "/courses";

            return "/";
        }

        private static bool IsLocalUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url)
            && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\");


        private sealed class InputModel
        {
            [Required(ErrorMessage = "E-post krävs")]
            [EmailAddress(ErrorMessage = "Ange en giltig e-postadress")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Lösenord krävs")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Kom ihåg mig?")]
            public bool RememberMe { get; set; }
        }

        private async Task<TokenDto?> GetTokensFromApi(string username, string password)
        {
            var client = HttpClientFactory.CreateClient("LmsAPIClient");
            var response = await client.PostAsJsonAsync("api/auth/login",
                new UserAuthDto { UserName = username, Password = password });

            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TokenDto>()
                : null;
        }
    }
}