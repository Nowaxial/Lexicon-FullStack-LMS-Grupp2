using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace LMS.Blazor.Client.Services;

public sealed class AuthReadyService : IAuthReadyService
{
    private readonly AuthenticationStateProvider _auth;
    private readonly TaskCompletionSource<bool> _readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    //Set max timeout here now 5s before error
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public AuthReadyService(AuthenticationStateProvider auth)
    {
        _auth = auth;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (!OperatingSystem.IsBrowser()) return;

        var state = await _auth.GetAuthenticationStateAsync();
        if (IsAuthenticated(state.User))
            _readyTcs.TrySetResult(true);

        _auth.AuthenticationStateChanged += async t =>
        {
            var state = await t;
            if (IsAuthenticated(state.User))
                _readyTcs.TrySetResult(true);
        };
    }

    private static bool IsAuthenticated(ClaimsPrincipal principal) =>
      principal.Identity?.IsAuthenticated == true;

    public async Task WaitAsync(CancellationToken ct = default)
    {
        var completed = await Task.WhenAny(_readyTcs.Task, Task.Delay(DefaultTimeout, ct));
        if (completed != _readyTcs.Task)
            throw new TimeoutException($"Auth was not ready within {DefaultTimeout}.");

        await _readyTcs.Task;
    }
}