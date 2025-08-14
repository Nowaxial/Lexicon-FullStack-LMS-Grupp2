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

    public async Task<IActionResult> Proxy(string endpoint)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        var accessToken = await _tokenService.GetAccessTokenAsync(userId);

        //ToDo: Before continue look for expired accesstoken and call refresh enpoint instead.
        //Tip: Look in TokenStorageService whats allready implementet
        //Use delegatinghandler on HttpClient or separate service to extract this logic!

        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized();

        var client = _httpClientFactory.CreateClient("LmsAPIClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var queryString = Request.QueryString.Value;
        var targetUriBuilder = new UriBuilder($"{client.BaseAddress}{endpoint}");
        if (!string.IsNullOrEmpty(queryString))
        {
            var queryParams = HttpUtility.ParseQueryString(queryString);
            queryParams.Remove("endpoint");

            targetUriBuilder.Query = queryParams.ToString();
        }

        var method = new HttpMethod(Request.Method);
        var requestMessage = new HttpRequestMessage(method, targetUriBuilder.Uri);

        if (method != HttpMethod.Get && Request.ContentLength > 0)
        {
            requestMessage.Content = new StreamContent(Request.Body);
        }

        foreach (var header in Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var response = await client.SendAsync(requestMessage);

        return !response.IsSuccessStatusCode
            ? Unauthorized()
            : StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }
}
