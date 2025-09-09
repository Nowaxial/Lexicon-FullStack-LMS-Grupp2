using LMS.Blazor.Client.Components.CourseComponents;
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

        private CancellationTokenSource? _docsCts;

        // download url helper (reuse your proxy)
        private static string BuildDownloadUrl(int id)
            => $"proxy?endpoint={Uri.EscapeDataString($"api/documents/{id}/download")}";

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


        //private async Task AddCourseAsync()
        //{
        //    var created = await ApiService.PostAsync<CourseDto>("api/courses", newCourse);
        //    if (created is null) return;

        //    courses ??= new List<CourseDto>();
        //    courses.Add(created);

        //    _courseNameCache[created.Id] = created.Name;

        //    newCourse = new CreateCourseDto
        //    {
        //        Starts = DateOnly.FromDateTime(DateTime.Today),
        //        Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        //    };

        //    await JS.InvokeVoidAsync(
        //        "eval",
        //        "bootstrap.Collapse.getOrCreateInstance(document.getElementById('addCourseTeacherPage')).hide()"
        //    );
        //}

        private Task LoadMyCoursesAsync() => LoadCoursesAsync();

        private async Task LoadCoursesAsync()
        {
            var data = await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses/my");

            Courses.Clear();
            foreach (var c in data ?? Enumerable.Empty<CourseDto>())
            {
                Courses.Add(new CourseItem(
                    Id: c.Id,
                    Title: c.Name,
                    Students: 0,
                    StartDate: c.Starts.ToDateTime(TimeOnly.MinValue)
                ));
                _courseNameCache[c.Id] = c.Name; 
            }
        }

        private async Task LoadUpcomingActivitiesAsync(CancellationToken ct = default)
        {
            Upcoming.Clear();

            var moduleFetches = Courses.Select(async course =>
            {
                var mods = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                    $"api/course/{course.Id}/Modules?includeActivities=false", ct
                ) ?? Enumerable.Empty<ModuleDto>();

                var activityFetches = mods.Select(async m =>
                {
                    var acts = await ApiService.CallApiAsync<IEnumerable<ProjActivityDto>>(
                        $"api/modules/{m.Id}/activities", ct
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
                    $"api/course/{course.Id}/Modules?includeActivities=false", ct
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
                    $"api/modules/{m.Id}/activities", ct
                ) ?? Enumerable.Empty<ProjActivityDto>();

                return new {Module = m, Count = acts.Count()};
            });

            var counted = await Task.WhenAll(countActivities);

            foreach(var c in  counted
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
                                        $"api/documents/by-course/{course.Id}", ct);
                    var usersTask = ApiService.CallApiAsync<IEnumerable<UserDto>>(
                                        $"api/courses/{course.Id}/users", ct);

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

                        // fetch status (or default "Ej bedömd")
                        var status = await GetStatusAsync(d.ActivityId ?? 0, studentId, ct);

                        items.Add(new DocItem(
                            Id: d.Id,
                            DisplayName: string.IsNullOrWhiteSpace(d.DisplayName) ? "Fil" : d.DisplayName!,
                            UploadedAt: d.UploadedAt,
                            CourseId: d.CourseId ?? course.Id,
                            ActivityId: d.ActivityId ?? 0,
                            StudentId: studentId,
                            StudentName: studentName,
                            Status: status
                        ));
                    }

                    return items;
                });

                var perCourseResults = await Task.WhenAll(perCourseTasks);

                // flatten + dedup by Id, then sort & cap
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
            "godkänd" or "approved" => "ok",
            "granskning" or "review" => "review",
            "underkänd" or "rejected" => "bad",
            _ => "pending" // Ej bedömd / default
        };
        private async Task<string> GetStatusAsync(int activityId, string? studentId, CancellationToken ct)
        {
            if (activityId <= 0 || string.IsNullOrWhiteSpace(studentId))
                return "Ej bedömd";

            try
            {
                // Example endpoint (adjust to your API):
                // returns a string like "Ej bedömd", "Granskning", "Godkänd", etc.
                var status = await ApiService.CallApiAsync<string>(
                    $"api/activities/{activityId}/submissions/{Uri.EscapeDataString(studentId!)}/status", ct);

                return string.IsNullOrWhiteSpace(status) ? "Ej bedömd" : status!;
            }
            catch
            {
                return "Ej bedömd";
            }
        }




    }
}
