using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

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

 
        //private List<CourseDto>? courses;

        //private CreateCourseDto newCourse = new()
        //{
        //    Starts = DateOnly.FromDateTime(DateTime.Today),
        //    Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        //};

        private List<ModuleDto> modules = new();

        private sealed record CourseItem(int Id, string Title, int Students, DateTime StartDate);

        private sealed record ModuleItem(string Title, int Items);
        private sealed record SubmissionItem(int Id, string Assignment, string Student, string Status);


        private readonly List<CourseItem> Courses = new();

        private readonly List<ModuleItem> Modules = new();

        private readonly List<ProjActivityDto> Upcoming = new();

   
        private readonly List<SubmissionItem> Submissions = new()
        {
            new(101,"Uppgift 1","Erik Svensson","Ej bedömd"),
            new(102,"Uppgift 2","Sara Nilsson","Granskning"),
            new(103,"Uppgift 3","Mikael Holm","Godkänd")
        };

        private readonly Dictionary<int, string> _courseNameCache = new();

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
                isLoading = true; // Flytta hit

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


    }
}
