using LMS.Blazor.Client.Services; // Använd Client-versionen
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;

namespace LMS.Blazor.Client.Handlers;

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IClientTokenStorage _tokenStorage; // Ändrat till IClientTokenStorage
    private readonly AuthenticationStateProvider _authStateProvider;

    public JwtAuthorizationMessageHandler(
        IClientTokenStorage tokenStorage, // Ändrat här också
        AuthenticationStateProvider authStateProvider)
    {
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();

        if (authState.User.Identity?.IsAuthenticated == true)
        {
            var userId = authState.User.FindFirst("sub")?.Value ??
                        authState.User.FindFirst("nameid")?.Value ??
                        authState.User.FindFirst("id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var token = await _tokenStorage.GetAccessTokenAsync(userId);

                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to retrieve token: {ex.Message}");
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
