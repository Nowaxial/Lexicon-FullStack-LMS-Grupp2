using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LMS.Blazor.Client.Components;

public partial class ManageCourses : ComponentBase
{
    [Parameter] public bool IsTeacher { get; set; } = false;
    [Inject] private IApiService ApiService { get; set; } = default!;

    private List<CourseDto>? courses;
    private CourseDto? selectedCourse;

    private CreateCourseDto newCourse = new()
    {
        Starts = DateOnly.FromDateTime(DateTime.Today),
        Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
    };

    private int? editingCourseId;
    private CourseEditModel courseEditModel = new();

    private List<UserDto>? users;
    private bool usersLoaded = false;
    private bool isLoadingUsers = false;
    private string? usersError;
    private string userFilter = string.Empty;

    protected override async Task OnInitializedAsync() => await LoadCoursesAsync();

    private async Task LoadCoursesAsync()
    {
        try
        {
            courses = (await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses"))?.ToList();

            if (courses != null && courses.Any())
            {
                await OnCourseSelected(courses.First());
            }
        }
        catch (Exception ex)
        {
            courses = new List<CourseDto>();
            Console.WriteLine("Failed to load courses: " + ex.Message);
        }
    }
    [Parameter] public EventCallback<ModuleDto> OnEditModuleRequested { get; set; }

    private async Task HandleEditModule(ModuleDto module)
    {
        // bubble up to Courses page
        await OnEditModuleRequested.InvokeAsync(module);
    }
    private async Task OnCourseSelected(CourseDto course)
    {
        var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
            $"api/course/{course.Id}/Modules");
        var moduleList = new List<ModuleDto>();
        if (modules != null)
        {
            foreach (var m in modules)
            {
                var activities = await ApiService.CallApiAsync<IEnumerable<ProjActivityDto>>(
                    $"api/modules/{m.Id}/activities");

                moduleList.Add(new ModuleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Starts = m.Starts,
                    Ends = m.Ends,
                    Activities = activities?.ToList() ?? new()
                });
            }
        }

        selectedCourse = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            Starts = course.Starts,
            Ends = course.Ends,
            Modules = moduleList
        };

        var idx = courses!.FindIndex(c => c.Id == course.Id);
        if (idx >= 0) courses[idx] = selectedCourse;
    }

    private async Task AddCourseAsync()
    {
        var created = await ApiService.PostAsync<CourseDto>("api/courses", newCourse);
        if (created != null)
        {
            courses ??= new List<CourseDto>();
            courses.Add(created);
            newCourse = new CreateCourseDto
            {
                Starts = DateOnly.FromDateTime(DateTime.Today),
                Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
            };
        }
    }

    private async Task DeleteSelectedCourse()
    {
        if (selectedCourse == null) return;
        var success = await ApiService.DeleteAsync($"api/courses/{selectedCourse.Id}");
        if (success)
        {
            courses?.Remove(selectedCourse);
            selectedCourse = null;
        }
    }

    private void StartEditCourse(CourseDto course)
    {
        editingCourseId = course.Id;
        courseEditModel = new CourseEditModel
        {
            Name = course.Name,
            Description = course.Description,
            Starts = course.Starts,
            Ends = course.Ends
        };
    }

    private async Task SaveCourse()
    {
        if (selectedCourse == null) return;

        var dto = new UpdateCourseDto
        {
            Name = courseEditModel.Name,
            Description = courseEditModel.Description,
            Starts = courseEditModel.Starts,
            Ends = courseEditModel.Ends
        };

        var success = await ApiService.PutAsync($"api/courses/{selectedCourse.Id}", dto);
        if (success)
        {
            await OnCourseSelected(selectedCourse);
        }

        editingCourseId = null;
    }

    private void CancelEditCourse()
    {
        editingCourseId = null;
        courseEditModel = new();
    }

    private IEnumerable<UserDto> FilteredUsers =>
        string.IsNullOrWhiteSpace(userFilter)
            ? (users ?? Enumerable.Empty<UserDto>())
            : (users ?? Enumerable.Empty<UserDto>())
                .Where(u =>
                    StartsWithIgnoreCase(u.FullName, userFilter) ||
                    StartsWithIgnoreCase(u.UserName, userFilter) ||
                    StartsWithIgnoreCase(u.Email, userFilter));

    private static bool StartsWithIgnoreCase(string? value, string prefix) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.IsNullOrWhiteSpace(prefix) &&
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private async Task LoadUsersIfNeeded()
    {
        if (usersLoaded || isLoadingUsers) return;
        await ReloadUsersAsync();
    }

    private async Task ReloadUsersAsync()
    {
        try
        {
            isLoadingUsers = true;
            usersError = null;
            var page = await ApiService.CallApiAsync<IEnumerable<UserDto>>("api/users");
            users = page?.ToList() ?? new List<UserDto>();
            usersLoaded = true;
        }
        catch (Exception ex)
        {
            usersError = "Failed to load users: " + ex.Message;
        }
        finally
        {
            isLoadingUsers = false;
        }
    }

    private class CourseEditModel
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }
    }
}