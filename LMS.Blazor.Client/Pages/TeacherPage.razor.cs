using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using UserDto = LMS.Shared.DTOs.UsersDtos.UserDto;

namespace LMS.Blazor.Client.Components.Pages
{
    public partial class TeacherPage
    {
        [Inject] private IApiService ApiService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private bool isLoading = true;
        private bool firstRenderDone;

        private bool showMoreUpcoming;

        private int modulesPage = 1;
        private const int ModulesPageSize = 4;

        private int activitiesPage = 1;
        private const int ActivitiesPageSize = 7;

        [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

        private string displayName = "Lärare";

        private List<ModuleDto> modules = new();

        // Feedback properties
        private string feedbackStatus = "Ej bedömd";
        private string feedbackText = "";
        private DocItem? currentDocument;
        private bool _savingFeedback = false;

        private sealed record CourseItem(int Id, string Title, int Students, DateTime StartDate);
        private sealed record ModuleItem(string Title, int Items);
        private sealed record SubmissionItem(int Id, string Assignment, string Student, string Status);

        private readonly List<CourseItem> Courses = new();
        private readonly List<ModuleItem> Modules = new();
        private readonly List<ProjActivityDto> Upcoming = new();
        private readonly List<ProjDocumentDto> RecentDocs = new();
        private readonly Dictionary<int, string> _courseNameCache = new();

        // ---- Documents (Nya inlämningar) ----
        private sealed record DocItem(
            int Id,
            string DisplayName,
            DateTime UploadedAt,
            int CourseId,
            int ActivityId,
            string StudentId,
            string StudentName,
            string Status);

        private readonly List<DocItem> _latestDocs = new();
        private bool _loadingDocs;

        private static string BuildDownloadUrl(int id)
            => $"proxy?endpoint={Uri.EscapeDataString($"api/documents/{id}/download")}";

        private static readonly DocumentStatus[] _statusOptions = new[]
         {
            DocumentStatus.Pending, DocumentStatus.Review, DocumentStatus.Approved, DocumentStatus.Rejected
        };

        private readonly HashSet<int> _savingDocIds = new();

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

        // Feedback methods
        private async Task OpenFeedbackModal(DocItem document)
        {
            currentDocument = document;

            if (document.Status.Contains(":"))
            {
                var parts = document.Status.Split(':', 2);
                feedbackStatus = parts[0].Trim();
                feedbackText = ""; // Töm för ny feedback
            }
            else
            {
                feedbackStatus = document.Status;
                feedbackText = "";
            }

            await JS.InvokeVoidAsync("eval", "new bootstrap.Modal(document.getElementById('feedbackModal')).show()");
        }

        private async Task SaveFeedbackAsync()
        {
            if (currentDocument == null) return;

            _savingFeedback = true;
            StateHasChanged();

            try
            {
                _savingDocIds.Add(currentDocument.Id);

                var status = ParseStatusToEnum(feedbackStatus);

                if (!string.IsNullOrEmpty(feedbackText))
                {
                    // Hantera feedback
                    var existingFeedback = GetExistingFeedback(currentDocument.Status);
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var newFeedbackEntry = $"[{timestamp}|{feedbackStatus}] {feedbackText}";

                    var allFeedback = string.IsNullOrEmpty(existingFeedback)
                        ? newFeedbackEntry
                        : $"{existingFeedback}\n{newFeedbackEntry}";

                    // Sätt statusen först
                    await ChangeStatusAsync(currentDocument, status);

                    // Använd SetStatusAsync MED feedback
                    await SetStatusWithFeedbackAsync(currentDocument.Id, status, allFeedback);

                    // Uppdatera lokal visning
                    var displayStatus = $"{feedbackStatus}: {allFeedback}";
                    var index = _latestDocs.FindIndex(d => d.Id == currentDocument.Id);
                    if (index >= 0)
                    {
                        _latestDocs[index] = currentDocument with { Status = displayStatus };
                    }
                }

                _savingDocIds.Remove(currentDocument.Id);
                await JS.InvokeVoidAsync("eval", "bootstrap.Modal.getInstance(document.getElementById('feedbackModal')).hide()");
            }
            catch (Exception ex)
            {
                await JS.InvokeVoidAsync("console.error", $"SaveFeedback failed: {ex}");
                await JS.InvokeVoidAsync("alert", "Kunde inte spara feedback. Försök igen.");
            }
            finally
            {
                _savingFeedback = false;
                StateHasChanged();
            }
        }

        // Lägg till denna nya metod
        private async Task SetStatusWithFeedbackAsync(int documentId, DocumentStatus status, string feedback)
        {
            var dto = new SetDocumentStatusDto
            {
                Status = status,
                Feedback = feedback
            };
            await ApiService.PostAsync<object?>($"api/documents/{documentId}/status", dto);
        }

        private string GetExistingFeedback(string status)
        {
            if (!status.Contains(":")) return "";
            return status.Split(':', 2)[1].Trim();
        }

        // Lägg till denna hjälpmetod
        private static DocumentStatus ParseStatusToEnum(string status)
        {
            var baseStatus = status.Contains(":") ? status.Split(':')[0].Trim() : status;
            return baseStatus.ToLowerInvariant() switch
            {
                "godkänd" => DocumentStatus.Approved,
                "underkänd" => DocumentStatus.Rejected,
                "granskning" => DocumentStatus.Review,
                _ => DocumentStatus.Pending
            };
        }

        private string CourseTitle(int courseId)
        {
            if (_courseNameCache.TryGetValue(courseId, out var name))
                return name;

            var found = Courses.FirstOrDefault(c => c.Id == courseId)?.Title;
            if (!string.IsNullOrWhiteSpace(found))
            {
                _courseNameCache[courseId] = found!;
                return found!;
            }

            return $"Kurs {courseId}";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRenderDone)
            {
                firstRenderDone = true;
                isLoading = true;

                if (AuthStateTask is not null)
                {
                    var auth = await AuthStateTask;
                    var firstName = auth.User.FindFirst("FirstName")?.Value;
                    var lastName = auth.User.FindFirst("LastName")?.Value;

                    displayName = !string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName)
                        ? $"{firstName} {lastName}".Trim()
                        : auth.User.Identity?.Name ?? displayName;
                }

                try
                {
                    await CallAPIAsync();
                }
                finally
                {
                    isLoading = false;
                    StateHasChanged();
                }
            }
        }

        private async Task CallAPIAsync()
        {
            await LoadMyCoursesAsync();
            await LoadUpcomingActivitiesAsync();
            await LoadModulesAsync();
            await LoadLatestDocsAsync();
        }

        private void ToggleUpcoming() => showMoreUpcoming = !showMoreUpcoming;

        private Task LoadMyCoursesAsync() => LoadCoursesAsync();

        private async Task LoadCoursesAsync()
        {
            var data = await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses/my");

            Courses.Clear();

            var courseTasks = (data ?? Enumerable.Empty<CourseDto>()).Select(async c =>
            {
                var users = await ApiService.CallApiAsync<IEnumerable<UserDto>>($"api/courses/{c.Id}/users");
                var usersList = users?.ToList() ?? new List<UserDto>();

                var studentCount = usersList.Count(u => u.IsTeacher != true);

                return new CourseItem(
                    Id: c.Id,
                    Title: c.Name,
                    Students: studentCount,
                    StartDate: c.Starts.ToDateTime(TimeOnly.MinValue)
                );
            });

            var courseItems = await Task.WhenAll(courseTasks);

            foreach (var item in courseItems)
            {
                Courses.Add(item);
                _courseNameCache[item.Id] = item.Title;
            }
        }

        private async Task LoadUpcomingActivitiesAsync(CancellationToken ct = default)
        {
            Upcoming.Clear();

            var moduleFetches = Courses.Select(async course =>
            {
                var mods = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                    $"api/course/{course.Id}/Modules?includeActivities=false"
                ) ?? Enumerable.Empty<ModuleDto>();

                var activityFetches = mods.Select(async m =>
                {
                    var acts = await ApiService.CallApiAsync<IEnumerable<ProjActivityDto>>(
                        $"api/modules/{m.Id}/activities"
                    ) ?? Enumerable.Empty<ProjActivityDto>();

                    var now = DateTime.UtcNow;
                    var future = acts.Where(a => a.Starts.ToUniversalTime() >= now);

                    foreach (var a in future)
                    {
                        if (a.CourseId == 0) a.CourseId = course.Id;
                        Upcoming.Add(a);
                    }
                });

                await Task.WhenAll(activityFetches);
            });

            await Task.WhenAll(moduleFetches);

            Upcoming.Sort((x, y) => DateTime.Compare(x.Starts, y.Starts));
            if (Upcoming.Count > 20)
                Upcoming.RemoveRange(20, Upcoming.Count - 20);
        }

        private async Task LoadModulesAsync(CancellationToken ct = default)
        {
            Modules.Clear();
            modules = new List<ModuleDto>();

            if (Courses.Count == 0) return;

            var moduleFetches = Courses.Select(async course =>
            {
                var mods = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                    $"api/course/{course.Id}/Modules?includeActivities=false"
                ) ?? Enumerable.Empty<ModuleDto>();

                foreach (var m in mods)
                {
                    modules.Add(m);
                }
            });

            await Task.WhenAll(moduleFetches);

            var countActivities = modules.Select(async m =>
            {
                var acts = await ApiService.CallApiAsync<IEnumerable<ProjActivityDto>>(
                    $"api/modules/{m.Id}/activities"
                ) ?? Enumerable.Empty<ProjActivityDto>();

                return new { Module = m, Count = acts.Count() };
            });

            var counted = await Task.WhenAll(countActivities);

            foreach (var c in counted
                .OrderBy(c => c.Module.Starts)
                .ThenBy(c => c.Module.Name))
            {
                Modules.Add(new ModuleItem(
                    Title: c.Module.Name ?? $"Modul {c.Module.Id}",
                   Items: c.Count
                 ));
            }
        }

        private async Task LoadLatestDocsAsync(CancellationToken ct = default)
        {
            _loadingDocs = true;
            try
            {
                _latestDocs.Clear();

                if (Courses.Count == 0)
                    return;

                // fetch docs + users for each course in parallel
                var perCourseTasks = Courses.Select(async course =>
                {
                    var docsTask = ApiService.CallApiAsync<IEnumerable<ProjDocumentDto>>(
                                        $"api/documents/by-course/{course.Id}");
                    var usersTask = ApiService.CallApiAsync<IEnumerable<UserDto>>(
                                        $"api/courses/{course.Id}/users");

                    var docs = await docsTask ?? Enumerable.Empty<ProjDocumentDto>();
                    var users = await usersTask ?? Enumerable.Empty<UserDto>();

                    var names = users.ToDictionary(
                        u => u.Id ?? string.Empty,
                        u => string.IsNullOrWhiteSpace(u.FirstName) && string.IsNullOrWhiteSpace(u.LastName)
                                ? (u.UserName ?? u.Email ?? u.Id ?? "Okänd")
                                : $"{u.FirstName} {u.LastName}".Trim());

                    var submissions = docs.Where(d => d.IsSubmission)
                                          .OrderByDescending(d => d.UploadedAt);

                    var items = new List<DocItem>();
                    foreach (var d in submissions)
                    {
                        ct.ThrowIfCancellationRequested();

                        var studentId = d.StudentId ?? string.Empty;
                        names.TryGetValue(studentId, out var studentName);
                        studentName ??= string.IsNullOrWhiteSpace(studentId) ? "Okänd" : studentId;

                        items.Add(new DocItem(
                            Id: d.Id,
                            DisplayName: string.IsNullOrWhiteSpace(d.DisplayName) ? "Fil" : d.DisplayName!,
                            UploadedAt: d.UploadedAt,
                            CourseId: d.CourseId ?? course.Id,
                            ActivityId: d.ActivityId ?? 0,
                            StudentId: studentId,
                            StudentName: studentName,
                            Status: d.Status // Keep original status with potential feedback
                        ));
                    }

                    return items;
                });

                var perCourseResults = await Task.WhenAll(perCourseTasks);

                _latestDocs.AddRange(
                    perCourseResults
                        .SelectMany(x => x)
                        .GroupBy(x => x.Id)
                        .Select(g => g.First())
                        .OrderByDescending(x => x.UploadedAt)
                        .ThenByDescending(x => x.Id)
                        .Take(25)
                );
            }
            catch (OperationCanceledException)
            {
                // ignore – a newer load likely started
            }
            finally
            {
                _loadingDocs = false;
                StateHasChanged();
            }
        }

        public Task RefreshLatestDocsAsync() => LoadLatestDocsAsync();

        private static string StatusCss(string? status) => (status ?? "").ToLowerInvariant() switch
        {
            var s when s.StartsWith("godkänd") || s.StartsWith("approved") => "ok",
            var s when s.StartsWith("granskning") || s.StartsWith("review") => "review",
            var s when s.StartsWith("underkänd") || s.StartsWith("rejected") => "bad",
            _ => "pending" // Ej bedömd / Pending
        };

        private async Task ChangeStatusAsync(DocItem doc, DocumentStatus newStatus)
        {
            if (doc is null) return;

            var current = ParseStatus(doc.Status);
            if (current == newStatus) return;

            var index = _latestDocs.FindIndex(x => x.Id == doc.Id);
            if (index < 0) return;

            var old = doc.Status;

            _savingDocIds.Add(doc.Id);
            _latestDocs[index] = doc with { Status = LabelFor(newStatus) };
            StateHasChanged();

            try
            {
                await SetStatusAsync(doc.Id, newStatus, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _latestDocs[index] = doc with { Status = old };
                await JS.InvokeVoidAsync("console.error", $"SetStatus failed for doc {doc.Id}: {ex}");
                await JS.InvokeVoidAsync("alert", "Kunde inte spara status. Försök igen.");
            }
            finally
            {
                _savingDocIds.Remove(doc.Id);
                StateHasChanged();
            }
        }

        private async Task SetStatusAsync(int documentId, DocumentStatus status, CancellationToken ct)
        {
            var dto = new SetDocumentStatusDto { Status = status };
            await ApiService.PostAsync<object?>($"api/documents/{documentId}/status", dto);
        }
    }
}