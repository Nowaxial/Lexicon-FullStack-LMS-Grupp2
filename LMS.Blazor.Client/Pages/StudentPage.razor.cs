using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using UserDto = LMS.Shared.DTOs.UsersDtos.UserDto;

namespace LMS.Blazor.Client.Components.Pages
{
    public partial class StudentPage
    {
        [Inject] private IApiService ApiService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

        private bool isLoading = true;
        private string displayName = "Student";
        private CourseDto? currentCourse;
        private List<ModuleDto> modules = new();
        private List<UserDto> courseStudents = new();
        private Dictionary<int, List<ProjActivityDto>> moduleActivities = new();
        private HashSet<int> expandedModules = new();
        private List<UserDto> courseTeachers = new();


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (AuthStateTask is not null)
                {
                    var auth = await AuthStateTask;
                    var firstName = auth.User.FindFirst("FirstName")?.Value;
                    var lastName = auth.User.FindFirst("LastName")?.Value;

                    displayName = !string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName)
                        ? $"{firstName} {lastName}".Trim()
                        : auth.User.Identity?.Name ?? displayName;
                }

                await LoadStudentDataAsync();
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadStudentDataAsync()
        {
            await LoadStudentCourseAsync();
            if (currentCourse != null)
            {
                await LoadCourseModulesAsync();
                await LoadCourseStudentsAsync();
                await LoadCourseTeachersAsync();
            }
        }

        


        private async Task LoadStudentCourseAsync()
        {
            var courses = await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses/my");
            currentCourse = courses?.FirstOrDefault();
        }

        private async Task LoadCourseModulesAsync()
        {
            if (currentCourse == null) return;

            var moduleData = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                $"api/course/{currentCourse.Id}/Modules?includeActivities=false");

            modules = moduleData?.ToList() ?? new List<ModuleDto>();
        }

        private async Task LoadCourseStudentsAsync()
        {
            if (currentCourse == null) return;

            var users = await ApiService.CallApiAsync<IEnumerable<UserDto>>(
                $"api/courses/{currentCourse.Id}/users");

            courseStudents = users?.Where(u => u.IsTeacher == false).ToList() ?? new List<UserDto>();
        }

        private async Task LoadCourseTeachersAsync()
        {
            if (currentCourse == null) return;

            var users = await ApiService.CallApiAsync<IEnumerable<UserDto>>(
                $"api/courses/{currentCourse.Id}/users");

            courseTeachers = users?.Where(u => u.IsTeacher == true).ToList() ?? new List<UserDto>();
        }


        private async Task ToggleModuleActivities(int moduleId)
        {
            if (expandedModules.Contains(moduleId))
            {
                expandedModules.Remove(moduleId);
            }
            else
            {
                expandedModules.Add(moduleId);

                if (!moduleActivities.ContainsKey(moduleId))
                {
                    var activities = await ApiService.CallApiAsync<IEnumerable<ProjActivityDto>>(
                        $"api/modules/{moduleId}/activities");

                    moduleActivities[moduleId] = activities?.ToList() ?? new List<ProjActivityDto>();
                }
            }
        }
    }
}
