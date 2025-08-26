using LMS.Blazor.Client.Services;

namespace LMS.Blazor.Services.NoOpService;

/// <summary>
/// No-op fallback implementation of IApiService for server-side render mode.
/// This ensures components can still compile & run without making actual API calls.
/// </summary>
public class ServerNoopApiService : IApiService
{
    public Task<T?> CallApiAsync<T>(string endpoint, CancellationToken ct = default)
    {
        // Always return default (null for reference types)
        return Task.FromResult<T?>(default);
    }

    public Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken ct = default)
    {
        // Pretend nothing was created
        return Task.FromResult<T?>(default);
    }

    public Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        // Pretend delete succeeded
        return Task.FromResult(true);
    }
    //Task<bool> IApiService.PutAsync(string endpoint, object data, CancellationToken ct)
    //{
    //    throw new NotImplementedException();
    //}
}