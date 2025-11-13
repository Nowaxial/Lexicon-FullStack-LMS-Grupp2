namespace LMS.Blazor.Client.Services;

public interface IClientTokenStorage
{
    Task<string?> GetAccessTokenAsync(string userId);
    Task SetAccessTokenAsync(string userId, string token);
    Task RemoveTokensAsync(string userId);
}
