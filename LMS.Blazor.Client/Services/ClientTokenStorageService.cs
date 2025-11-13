using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Services;

public class ClientTokenStorageService : IClientTokenStorage
{
    private readonly IJSRuntime _jsRuntime;

    public ClientTokenStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAccessTokenAsync(string userId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"access_token_{userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting token: {ex.Message}");
            return null;
        }
    }

    public async Task SetAccessTokenAsync(string userId, string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"access_token_{userId}", token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting token: {ex.Message}");
        }
    }

    public async Task RemoveTokensAsync(string userId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"access_token_{userId}");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"refresh_token_{userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing tokens: {ex.Message}");
        }
    }
}
