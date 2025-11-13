using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Components.CourseComponents
{
    public partial class StudentDocuments : IDisposable
    {

        [Inject] private IApiService Api { get; set; } = default!;
        [Inject] private DocumentsClient DocsClient { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;


        [Parameter] public int? CourseId { get; set; }
        [Parameter] public int? ModuleId { get; set; }
        [Parameter] public int? ActivityId { get; set; }
        [Parameter] public string? StudentId { get; set; }
        [Parameter] public bool AutoLoad { get; set; } = true;


        private bool _isLoading = true;
        private string? _error;
        private List<ProjDocumentDto> _docs = new();

        private CancellationTokenSource? _cts;
        private string? _lastEndpoint;

        private readonly HashSet<int> _deleting = new();

        private readonly Dictionary<int, string> _moduleNames = new();

        private readonly Dictionary<int, string> _activityNames = new();

        private string ModuleName(int? moduleId)
        {
            if (!moduleId.HasValue) return "Okänd modul";
            return _moduleNames.TryGetValue(moduleId.Value, out var name)
                ? name
                : $"Modul {moduleId.Value}";
        }

        private string ActivityName(int? activityId)
        {
            if (!activityId.HasValue) return "Okänd aktivitet";
            return _activityNames.TryGetValue(activityId.Value, out var name)
                ? name
                : $"Aktivitet {activityId.Value}";
        }


        private static string LabelFor(DocumentStatus s) => s switch
        {
            DocumentStatus.Pending => "Ej bedömd",
            DocumentStatus.Review => "Granskning",
            DocumentStatus.Approved => "Godkänd",
            DocumentStatus.Rejected => "Underkänd",
            _ => s.ToString()
        };

        private static DocumentStatus ParseStatus(string? value)
        {
            var v = (value ?? "").Trim().ToLowerInvariant();

            // Handle feedback format "Status: feedback text"
            if (v.Contains(":"))
            {
                v = v.Split(':')[0].Trim();
            }

            return v switch
            {
                "ej bedömd" or "pending" => DocumentStatus.Pending,
                "granskning" or "review" => DocumentStatus.Review,
                "godkänd" or "approved" => DocumentStatus.Approved,
                "underkänd" or "rejected" => DocumentStatus.Rejected,
                _ => DocumentStatus.Pending
            };
        }

        private static string StatusCss(string? status) => (status ?? "").ToLowerInvariant() switch
        {
            var s when s.StartsWith("godkänd") || s.StartsWith("approved") => "ok",
            var s when s.StartsWith("granskning") || s.StartsWith("review") => "review",
            var s when s.StartsWith("underkänd") || s.StartsWith("rejected") => "bad",
            _ => "pending"
        };

        protected override async Task OnParametersSetAsync()
        {
            if (AutoLoad)
                await ReloadAsync();
        }


        public Task RefreshAsync() => ReloadAsync(force: true);

        public async Task ReloadAsync(bool force = false)
        {
            _error = null;

            var endpoint = BuildEndpoint();
            if (endpoint is null)
            {
                _docs = new();
                _isLoading = false;
                StateHasChanged();
                return;
            }

            if (!force && string.Equals(endpoint, _lastEndpoint, StringComparison.OrdinalIgnoreCase))
                return;

            _lastEndpoint = endpoint;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _isLoading = true;
            StateHasChanged();

            try
            {
                var result = await Api.CallApiAsync<IEnumerable<ProjDocumentDto>>(endpoint, _cts.Token);

                var docs = (result ?? Enumerable.Empty<ProjDocumentDto>());

                if (!string.IsNullOrWhiteSpace(StudentId))
                    docs = docs.Where(d => string.Equals(d.StudentId ?? "", StudentId, StringComparison.OrdinalIgnoreCase));

                docs = docs.Where(d => d.IsSubmission);

                _docs = docs
                    .OrderByDescending(d => d.UploadedAt)
                    .ThenByDescending(d => d.Id)
                    .Take(50)
                    .ToList();

                await EnsureModuleNamesAsync(_docs, _cts.Token);
                await EnsureActivityNamesAsync(_docs, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignore—newer load started
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _docs = new();
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }


        private async Task EnsureModuleNamesAsync(IEnumerable<ProjDocumentDto> docs, CancellationToken ct = default)
        {
            var needed = docs
                .Select(d => d.ModuleId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Where(id => !_moduleNames.ContainsKey(id))
                .ToList();

            if (needed.Count == 0) return;

            try
            {
                if (CourseId.HasValue)
                {
                    var mods = await Api.CallApiAsync<IEnumerable<ModuleDto>>(
                        $"api/course/{CourseId.Value}/Modules?includeActivities=false", ct);

                    foreach (var m in mods ?? Enumerable.Empty<ModuleDto>())
                        _moduleNames[m.Id] = m.Name;
                }
                else
                {
                    foreach (var id in needed)
                    {
                        var mod = await Api.CallApiAsync<ModuleDto>($"api/modules/{id}", ct);
                        if (mod is not null)
                            _moduleNames[id] = mod.Name;
                    }
                }
            }
            catch
            {

            }
        }

        private async Task EnsureActivityNamesAsync(IEnumerable<ProjDocumentDto> docs, CancellationToken ct = default)
        {
            var needed = docs
                .Select(d => d.ActivityId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Where(id => !_activityNames.ContainsKey(id))
                .ToList();

            if (needed.Count == 0) return;

            try
            {
                if (ModuleId.HasValue)
                {
                    var acts = await Api.CallApiAsync<IEnumerable<ProjActivityDto>>(
                        $"api/Modules/{ModuleId.Value}/Activities", ct);

                    foreach (var a in acts ?? Enumerable.Empty<ProjActivityDto>())
                        _activityNames[a.Id] = a.Title;
                }
                else if (CourseId.HasValue)
                {
                    var modules = await Api.CallApiAsync<IEnumerable<ModuleDto>>(
                        $"api/course/{CourseId.Value}/Modules?includeActivities=false", ct);

                    foreach (var m in modules ?? Enumerable.Empty<ModuleDto>())
                    {
                        var acts = await Api.CallApiAsync<IEnumerable<ProjActivityDto>>(
                            $"api/modules/{m.Id}/activities", ct);
                        foreach (var a in acts ?? Enumerable.Empty<ProjActivityDto>())
                            if (!_activityNames.ContainsKey(a.Id))
                                _activityNames[a.Id] = a.Title;
                    }
                }
                else
                {
                    foreach (var id in needed)
                    {
                        var acts = await Api.CallApiAsync<ProjActivityDto>($"api/activities/{id}", ct);
                        if (acts is not null)
                            _activityNames[id] = acts.Title;
                    }
                }
            }
            catch
            {

            }
        }
        private string? BuildEndpoint()
        {
            if (ActivityId.HasValue)
                return $"api/documents/by-activity/{ActivityId.Value}";
            if (ModuleId.HasValue)
                return $"api/documents/by-module/{ModuleId.Value}";
            if (CourseId.HasValue)
                return $"api/documents/by-course/{CourseId.Value}";
            if (!string.IsNullOrWhiteSpace(StudentId))
                return $"api/documents/by-student/{Uri.EscapeDataString(StudentId!)}";

            return null;
        }

        internal static string BuildDownloadUrl(int id)
            => $"proxy?endpoint={Uri.EscapeDataString($"api/documents/{id}/download")}";

        private static string GetExt(string? path)
        => string.IsNullOrWhiteSpace(path) ? "" : Path.GetExtension(path).TrimStart('.').ToUpperInvariant();

        private void Download(int id)
            => Nav.NavigateTo(BuildDownloadUrl(id), forceLoad: true);

        private async Task DeleteAsync(ProjDocumentDto doc)
        {
            try
            {
                var okAsk = await JS.InvokeAsync<bool>("confirm",
               $"Ta bort \"{doc.DisplayName}\"?");
                if (!okAsk) return;

                _deleting.Add(doc.Id);
                StateHasChanged();

                var ok = await DocsClient.DeleteAsync(doc.Id);
                if (ok)
                {
                    _docs.RemoveAll(d => d.Id == doc.Id);
                }
                else
                {
                    _error = "Kunde inte ta bort filen (saknas eller behörighet saknas).";
                }
            }
            catch (HttpRequestException ex)
            {
                _error = $"Borttagning misslyckades: {ex.Message}";
            }
            catch (Exception ex)
            {
                _error = $"Ett fel inträffade: {ex.Message}";
            }
            finally
            {
                _deleting.Remove(doc.Id);
                StateHasChanged();
            }
        }

        private static string GetFeedbackCss(string status)
        {
            var s = status.ToLowerInvariant();
            if (s.StartsWith("godkänd") || s.StartsWith("approved"))
                return "feedback-approved";
            if (s.StartsWith("underkänd") || s.StartsWith("rejected"))
                return "feedback-rejected";
            if (s.StartsWith("granskning") || s.StartsWith("review"))
                return "feedback-review";
            return "feedback-pending";
        }


        private List<FeedbackEntry> ParseFeedbackHistory(string status)
        {
            if (!status.Contains(":")) return new List<FeedbackEntry>();

            var feedbackText = status.Split(':', 2)[1].Trim();
            var entries = new List<FeedbackEntry>();

            // Split by lines and parse each feedback entry
            var lines = feedbackText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                // Check if line starts with timestamp format [yyyy-MM-dd HH:mm:ss|Status] or [yyyy-MM-dd HH:mm|Status]
                if (trimmedLine.StartsWith("[") && trimmedLine.Contains("]"))
                {
                    var endBracket = trimmedLine.IndexOf(']');
                    if (endBracket > 0)
                    {
                        var headerStr = trimmedLine.Substring(1, endBracket - 1);
                        var feedbackContent = trimmedLine.Substring(endBracket + 1).Trim();

                        // Parse timestamp and status
                        if (headerStr.Contains("|"))
                        {
                            var parts = headerStr.Split('|', 2);
                            var timestampStr = parts[0];
                            var entryStatus = parts[1];

                            if (DateTime.TryParse(timestampStr, out var timestamp))
                            {
                                entries.Add(new FeedbackEntry(timestamp, feedbackContent, entryStatus));
                            }
                            else
                            {
                                entries.Add(new FeedbackEntry(DateTime.Now, trimmedLine, "Ej bedömd"));
                            }
                        }
                        else
                        {
                            // Old format without status
                            if (DateTime.TryParse(headerStr, out var timestamp))
                            {
                                entries.Add(new FeedbackEntry(timestamp, feedbackContent, "Ej bedömd"));
                            }
                            else
                            {
                                entries.Add(new FeedbackEntry(DateTime.Now, trimmedLine, "Ej bedömd"));
                            }
                        }
                    }
                }
                else
                {
                    // Old format without timestamp
                    entries.Add(new FeedbackEntry(DateTime.Now, trimmedLine, "Ej bedömd"));
                }
            }

            return entries.OrderByDescending(e => e.Timestamp).ToList();
        }

        private record FeedbackEntry(DateTime Timestamp, string Content, string Status);


        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
