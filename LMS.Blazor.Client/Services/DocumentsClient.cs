using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace LMS.Blazor.Client.Services;

public sealed class DocumentsClient
{
    private readonly HttpClient _http;
    private readonly NavigationManager _nav;
    private readonly IAuthReadyService _authReady;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DocumentsClient(IHttpClientFactory factory, NavigationManager nav, IAuthReadyService authReady)
    {
        _http = factory.CreateClient("BffClient");
        _nav = nav;
        _authReady = authReady;
    }

    // ---------- Public API ----------

    public Task<ProjDocumentDto?> UploadToActivityAsync(
        int activityId,
        UploadProjDocumentDto meta,
        IBrowserFile file,
        long maxBytes,
        CancellationToken ct = default) =>
        UploadAsync($"api/activities/{activityId}/documents", meta, file, maxBytes, ct);

    public Task<ProjDocumentDto?> UploadToModuleAsync(
        int moduleId,
        UploadProjDocumentDto meta,
        IBrowserFile file,
        long maxBytes,
        CancellationToken ct = default) =>
        UploadAsync($"api/modules/{moduleId}/documents", meta, file, maxBytes, ct);

    public Task<ProjDocumentDto?> UploadToCourseAsync(
        int courseId,
        UploadProjDocumentDto meta,
        IBrowserFile file,
        long maxBytes,
        CancellationToken ct = default) =>
        UploadAsync($"api/courses/{courseId}/documents", meta, file, maxBytes, ct);

    // ---------- Core upload ----------

    public async Task<ProjDocumentDto?> UploadAsync(
        string endpoint,
        UploadProjDocumentDto meta,
        IBrowserFile file,
        long maxBytes,
        CancellationToken ct)
    {
        await _authReady.WaitAsync(ct);

        using var content = BuildMultipart(meta, file, maxBytes, ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"proxy?endpoint={endpoint}")
        {
            Content = content
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (HandleUnauthorized(response))
            return default;

        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        var payload = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<ProjDocumentDto>(payload, JsonOpts);
    }

    // ---------- Helpers ----------

    private static MultipartFormDataContent BuildMultipart(
        UploadProjDocumentDto meta,
        IBrowserFile file,
        long maxBytes,
        CancellationToken ct)
    {
        var form = new MultipartFormDataContent();

        if (!string.IsNullOrWhiteSpace(meta.DisplayName))
            form.Add(new StringContent(meta.DisplayName, Encoding.UTF8), "displayName");

        if (!string.IsNullOrWhiteSpace(meta.Description))
            form.Add(new StringContent(meta.Description, Encoding.UTF8), "description");

        form.Add(new StringContent(meta.IsSubmission ? "true" : "false"), "isSubmission");

        if (meta.CourseId.HasValue)
            form.Add(new StringContent(meta.CourseId.Value.ToString()), "courseId");

        if (meta.ModuleId.HasValue)
            form.Add(new StringContent(meta.ModuleId.Value.ToString()), "moduleId");

        if (meta.ActivityId.HasValue)
            form.Add(new StringContent(meta.ActivityId.Value.ToString()), "activityId");

        if (!string.IsNullOrWhiteSpace(meta.StudentId))
            form.Add(new StringContent(meta.StudentId), "studentId");

        // Binary
        var stream = file.OpenReadStream(maxAllowedSize: maxBytes, cancellationToken: ct);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        form.Add(fileContent, "file", file.Name);

        return form;
    }

    private bool HandleUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var returnUrl = Uri.EscapeDataString(_nav.Uri);
            _nav.NavigateTo($"authentication/login?returnUrl={returnUrl}", forceLoad: true);
            return true;
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _nav.NavigateTo("AccessDenied", forceLoad: true);
            return true;
        }
        return false;
    }
}
