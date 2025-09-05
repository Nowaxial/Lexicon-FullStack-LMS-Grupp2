using LMS.Blazor.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;

namespace LMS.Blazor.Controller;

[Route("proxy")]
[ApiController]
public class ProxyController(IHttpClientFactory httpClientFactory, ITokenStorage tokenService) : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenStorage _tokenService = tokenService;

    // Handle GET, POST, PUT, DELETE
    [HttpGet, HttpPost, HttpPut, HttpDelete]
    public async Task<IActionResult> Proxy([FromQuery] string endpoint, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        var client = _httpClientFactory.CreateClient("LmsAPIClient");

        // Allow contact endpoint without authentication
        if (!endpoint.Equals("api/contact", StringComparison.OrdinalIgnoreCase))
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var accessToken = await _tokenService.GetAccessTokenAsync(userId);
            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // --- Split endpoint path + query
        var parts = endpoint.Split('?', 2);
        var path = parts[0];
        var query = parts.Length > 1 ? parts[1] : string.Empty;

        // Merge with query params in current request (minus "endpoint")
        var queryParams = HttpUtility.ParseQueryString(Request.QueryString.Value ?? string.Empty);
        queryParams.Remove("endpoint");

        if (!string.IsNullOrEmpty(query))
        {
            var endpointQuery = HttpUtility.ParseQueryString(query);
            foreach (var key in endpointQuery.AllKeys)
            {
                queryParams[key] = endpointQuery[key];
            }
        }

        // Build target URI
        var targetUriBuilder = new UriBuilder($"{client.BaseAddress}{path}")
        {
            Query = queryParams.ToString()
        };

        // Forward request
        var method = new HttpMethod(Request.Method);
        var requestMessage = new HttpRequestMessage(method, targetUriBuilder.Uri);

        if (method != HttpMethod.Get && Request.ContentLength > 0)
        {
            requestMessage.Content = new StreamContent(Request.Body);
            foreach (var header in Request.Headers)
            {
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        foreach (var header in Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        var stream = await response.Content.ReadAsStreamAsync(ct);

        return File(stream, contentType, enableRangeProcessing: false);
    }
}