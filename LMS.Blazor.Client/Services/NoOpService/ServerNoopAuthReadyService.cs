using LMS.Blazor.Client.Services;

namespace LMS.Blazor.Services.NoOpService;

public sealed class ServerNoopAuthReadyService : IAuthReadyService
{
    public Task WaitAsync(CancellationToken ct = default) => Task.CompletedTask;
}