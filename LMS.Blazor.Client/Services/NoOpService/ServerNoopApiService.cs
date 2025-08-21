using LMS.Blazor.Client.Services;

namespace LMS.Blazor.Services.NoOpService;

public class ServerNoopApiService : IApiService
{
    public Task<T?> CallApiAsync<T>(string endpoint, CancellationToken ct = default) => Task.FromResult<T?>(default);
}