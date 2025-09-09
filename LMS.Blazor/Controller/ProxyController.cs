using LMS.Blazor.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;

namespace LMS.Blazor.Controller;

[Route("proxy")]
[ApiController]
[IgnoreAntiforgeryToken]
public class ProxyController(IHttpClientFactory httpClientFactory, ITokenStorage tokenService, ILogger<ProxyController> logger) : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenStorage _tokenService = tokenService;
    private readonly ILogger<ProxyController> _logger = logger;

    private static readonly HashSet<string> HopByHop = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Proxy-Connection","Expect"
    };

    [HttpGet("download-document/{id}")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var accessToken = await _tokenService.GetAccessTokenAsync(userId);
            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized();

            using var client = _httpClientFactory.CreateClient("LmsAPIClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"api/documents/{id}/download");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                return File(content, contentType, fileName);
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {Id}", id);
            return StatusCode(500);
        }
    }

    // Support GET, POST, PUT, DELETE automatically
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    [RequestSizeLimit(200L * 1024 * 1024)]
    public async Task<IActionResult> Proxy([FromQuery] string endpoint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return BadRequest("Missing endpoint.");

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

        var baseRoot = new Uri(client.BaseAddress!, "/");
        var targetUri = new Uri(baseRoot, "/" + endpoint.TrimStart('/'));
        var qs = HttpUtility.ParseQueryString(Request.QueryString.Value ?? "");
        qs.Remove("endpoint");
        var ub = new UriBuilder(targetUri) { Query = qs.ToString() };
        targetUri = ub.Uri;

        var method = new HttpMethod(Request.Method);
        using var forward = new HttpRequestMessage(method, targetUri);

        // Build target URI
        foreach (var (key, value) in Request.Headers)
        {
            if (key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
            if (HopByHop.Contains(key)) continue;
            forward.Headers.TryAddWithoutValidation(key, value.AsEnumerable());
        }

        if (method != HttpMethod.Get && method != HttpMethod.Head)
        {
            if (!string.IsNullOrEmpty(Request.ContentType) &&
                Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                // Allow reread and parse form
                Request.EnableBuffering();
                Request.Body.Position = 0;

                var form = await Request.ReadFormAsync(ct);

                // Rebuild multipart content from parsed fields/files
                var multi = new MultipartFormDataContent();

                // Copy simple fields
                foreach (var kvp in form)
                {
                    foreach (var val in kvp.Value)
                    {
                        multi.Add(new StringContent(val), kvp.Key);
                    }
                }

                // Copy files (this is what the API binder needs)
                foreach (var file in form.Files)
                {
                    var fileStream = file.OpenReadStream();
                    var sc = new StreamContent(fileStream);
                    if (!string.IsNullOrEmpty(file.ContentType))
                    {
                        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    }

                    multi.Add(sc, file.Name, file.FileName);
                }

                forward.Content = multi;
            }
            else
            {
                Request.EnableBuffering();
                Request.Body.Position = 0;

                var buffer = new MemoryStream();
                await Request.Body.CopyToAsync(buffer, ct);
                buffer.Position = 0;

                var content = new StreamContent(buffer);

                if (!string.IsNullOrEmpty(Request.ContentType))
                {
                    content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
                }

                forward.Content = content;
            }
        }

        using var response = await client.SendAsync(forward, HttpCompletionOption.ResponseHeadersRead, ct);

        Response.StatusCode = (int)response.StatusCode;

        foreach (var (k, v) in response.Headers)
            if (!HopByHop.Contains(k)) Response.Headers[k] = v.ToArray();
        foreach (var (k, v) in response.Content.Headers)
            if (!HopByHop.Contains(k)) Response.Headers[k] = v.ToArray();

        Response.Headers.Remove("transfer-encoding");
        Response.Headers["X-Proxy-Target"] = targetUri.ToString();

        await response.Content.CopyToAsync(Response.Body, ct);
        return new EmptyResult();
    }
}
