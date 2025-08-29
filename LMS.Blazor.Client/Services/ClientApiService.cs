using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace LMS.Blazor.Client.Services;

public class ClientApiService(IHttpClientFactory httpClientFactory, NavigationManager navigationManager, IAuthReadyService authReady) : IApiService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("BffClient");

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private bool HandleUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Send them to login and stop processing
            var returnUrl = Uri.EscapeDataString(navigationManager.Uri);
            navigationManager.NavigateTo($"authentication/login?returnUrl={returnUrl}", forceLoad: true);
            return true; // handled
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            navigationManager.NavigateTo("AccessDenied", forceLoad: true);
            return true; // handled
        }
        return false; // not handled
    }

    // ---------------- GET ----------------
    public async Task<T?> CallApiAsync<T>(string endpoint, CancellationToken ct = default)
    {
        await authReady.WaitAsync();

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"proxy?endpoint={endpoint}");
        var response = await httpClient.SendAsync(requestMessage, ct);

        if (HandleUnauthorized(response)) return default;   // for T? methods
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        response.EnsureSuccessStatusCode();

        // Handle empty/whitespace payloads safely
        var payload = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }

    // ---------------- POST ----------------
    public async Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken ct = default)
    {
        await authReady.WaitAsync();

        var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"proxy?endpoint={endpoint}")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(requestMessage, ct);

        if (HandleUnauthorized(response)) return default;
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        response.EnsureSuccessStatusCode();

        // Handle empty/whitespace payloads safely
        var payload = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }

    //// ---------------- PUT ----------------
    //public async Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken ct = default)
    //{
    //    await authReady.WaitAsync();

    //    var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
    //    var content = new StringContent(json, Encoding.UTF8, "application/json");

    //    var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"proxy?endpoint={endpoint}")
    //    {
    //        Content = content
    //    };

    //    var response = await httpClient.SendAsync(requestMessage, ct);
    //    response.EnsureSuccessStatusCode();

    //    return await JsonSerializer.DeserializeAsync<T>(
    //        await response.Content.ReadAsStreamAsync(),
    //        _jsonSerializerOptions,
    //        ct
    //    );
    //}

    // ---------------- DELETE ----------------
    public async Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        await authReady.WaitAsync();

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"proxy?endpoint={endpoint}");
        var response = await httpClient.SendAsync(requestMessage, ct);

        if (HandleUnauthorized(response)) return default;   // for T? methods
        response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode;
    }
}