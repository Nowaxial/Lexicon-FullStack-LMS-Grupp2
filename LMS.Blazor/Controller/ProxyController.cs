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

    // Support GET, POST, PUT, DELETE automatically
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
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

        // Build target URI
        var queryString = Request.QueryString.Value;
        var targetUriBuilder = new UriBuilder($"{client.BaseAddress}{endpoint}");
        if (!string.IsNullOrEmpty(queryString))
        {
            var queryParams = HttpUtility.ParseQueryString(queryString);
            queryParams.Remove("endpoint"); // don't forward "endpoint" itself
            targetUriBuilder.Query = queryParams.ToString();
        }

        // Forward request
        var method = new HttpMethod(Request.Method);
        var requestMessage = new HttpRequestMessage(method, targetUriBuilder.Uri);

        if (method != HttpMethod.Get && Request.ContentLength > 0)
        {
            requestMessage.Content = new StreamContent(Request.Body);

            // Copy content headers (so Content-Type like application/json is preserved)
            foreach (var header in Request.Headers)
            {
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        // Copy regular headers
        foreach (var header in Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) // skip content headers (already copied)
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var response = await client.SendAsync(requestMessage, ct);

        var content = await response.Content.ReadAsStringAsync();
        return StatusCode((int)response.StatusCode, content);
    }
}
