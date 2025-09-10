using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LMS.Blazor.Client.Services;

public class ClientApiService(
    IHttpClientFactory httpClientFactory,
    NavigationManager navigationManager,
    IAuthReadyService authReady
) : IApiService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("BffClient");

    // Unified serializer config
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // --- Helpers ---
    private static string WrapEndpoint(string endpoint)
    {
        var parts = endpoint.Split('?', 2);
        var path = Uri.EscapeDataString(parts[0]);

        // ✅ Use & instead of ? so query parameters don’t get swallowed inside `endpoint`
        return parts.Length == 2
            ? $"proxy?endpoint={path}&{parts[1]}"
            : $"proxy?endpoint={path}";
    }

    private bool HandleUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var returnUrl = Uri.EscapeDataString(navigationManager.Uri);
            navigationManager.NavigateTo($"authentication/login?returnUrl={returnUrl}", forceLoad: true);
            return true;
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            navigationManager.NavigateTo("AccessDenied", forceLoad: true);
            return true;
        }
        return false;
    }

    // ---------------- GET ----------------
    public async Task<T?> CallApiAsync<T>(string endpoint, CancellationToken ct = default)
    {
        await authReady.WaitAsync();

        var response = await httpClient.GetAsync(WrapEndpoint(endpoint), ct);

        if (HandleUnauthorized(response)) return default;
        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        response.EnsureSuccessStatusCode();

        // Handle empty/whitespace payloads safely
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }

    // ---------------- POST ----------------
    public async Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken ct = default)
    {
        await authReady.WaitAsync(ct);

        var response = await httpClient.PostAsJsonAsync(
            WrapEndpoint(endpoint), data, _jsonSerializerOptions, ct);

        if (HandleUnauthorized(response)) return default;

        response.EnsureSuccessStatusCode();

        // Bail out early if there's nothing to read
        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }

    // ---------------- PUT ----------------
    public async Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken ct = default)
    {
        await authReady.WaitAsync(ct);

        var response = await httpClient.PutAsJsonAsync(
            WrapEndpoint(endpoint), data, _jsonSerializerOptions, ct);

        if (HandleUnauthorized(response)) return default;

        response.EnsureSuccessStatusCode();

        //  Bail out early if there's nothing to read
        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }

    // Non-generic version if you only care about success/failure
    public async Task<bool> PutAsync(string endpoint, object data, CancellationToken ct = default)
    {
        await authReady.WaitAsync();

        var response = await httpClient.PutAsJsonAsync(WrapEndpoint(endpoint), data, _jsonSerializerOptions, ct);

        if (HandleUnauthorized(response)) return false;

        return response.IsSuccessStatusCode;
    }

    // ---------------- DELETE ----------------
    public async Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        await authReady.WaitAsync();

        var response = await httpClient.DeleteAsync(WrapEndpoint(endpoint), ct);

        if (HandleUnauthorized(response)) return false;

        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }
}