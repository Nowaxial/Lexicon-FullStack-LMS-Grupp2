using LMS.Shared.DTOs.AuthDtos;
using System.Collections.Concurrent;
using Domain.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace LMS.Blazor.Services;

//Only reason for returning Task is to make the class easier to write a new implementation for production
public class TokenStorageService : ITokenStorage
{
    private readonly IHttpClientFactory httpClientFactory;
    private static readonly ConcurrentDictionary<string, TokenDto> tokenStore = new();

    public TokenStorageService(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public Task StoreTokensAsync(string userId, TokenDto tokens)
    {
        tokenStore[userId] = tokens;
        return Task.CompletedTask;
    }

    public Task<TokenDto?> GetTokensAsync(string userId)
    {
        tokenStore.TryGetValue(userId, out var tokens);
        return Task.FromResult(tokens);
    }

    public Task<bool> RemoveTokensAsync(string userId)
    {
        return Task.FromResult(tokenStore.Remove(userId, out _));
    }

    public Task<string?> GetAccessTokenAsync(string userId)
    {
        tokenStore.TryGetValue(userId, out var tokens);
        return Task.FromResult(tokens?.AccessToken);
    }

    public async Task RefreshTokensAsync(string userId)
    {
        if (!tokenStore.TryGetValue(userId, out var tokens) || tokens?.RefreshToken == null)
        {
            throw new InvalidOperationException("No refresh token available.");
        }

        var client = httpClientFactory.CreateClient("APIClient");
        var response = await client.PostAsJsonAsync("api/token/refresh", new TokenDto(tokens.AccessToken, tokens.RefreshToken));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to refresh tokens.");
        }

        var newTokens = await response.Content.ReadFromJsonAsync<TokenDto>();
        if (newTokens == null)
        {
            throw new Exception("Invalid token response from API.");
        }

        await StoreTokensAsync(userId, newTokens);
    }
}

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrEmpty(user.FirstName))
            identity.AddClaim(new Claim("FirstName", user.FirstName));

        if (!string.IsNullOrEmpty(user.LastName))
            identity.AddClaim(new Claim("LastName", user.LastName));

        return identity;
    }
}
