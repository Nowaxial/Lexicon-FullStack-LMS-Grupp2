using LMS.Blazor.Client.Components.CourseComponents;
using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
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

        private UploadFileModal? _uploadModal;

        private StudentDocuments? _docsList;

        private string? _userId;
        private readonly HashSet<int> completedActivityIds = new();

        private const string LABEL_DONE = "Klar";
        private const string LABEL_WAITING = "Väntar";
        private const string LABEL_POSTPONED = "Försenad";



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

                    _userId =
                        auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        auth.User.FindFirst("sub")?.Value ??
                        auth.User.FindFirst("Id")?.Value ??
                        auth.User.Identity?.Name;
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

                await LoadApprovedCompletionsForModuleAsync(moduleId);
            }
        }


        private async Task OpenUploadFor(ProjActivityDto activity)
        {
            if (_uploadModal is null) return;

            if (currentCourse?.Id is null || activity?.ModuleId is null || activity?.Id is null)
            {
                return;
            }

            _uploadModal.CourseId = currentCourse.Id;
            _uploadModal.ModuleId = activity.ModuleId;
            _uploadModal.ActivityId = activity.Id;

            _uploadModal.IsSubmission = true;

            await _uploadModal.ShowAsync();
        }

        private async Task RefreshDocumentsAsync()
        {
            if (_docsList is not null)
                await _docsList.RefreshAsync();

            foreach (var moduleId in expandedModules.ToList())
            {
                await LoadApprovedCompletionsForModuleAsync(moduleId, force: true);
            }

            StateHasChanged();
        }

        private async Task LoadApprovedCompletionsForModuleAsync(int moduleId, bool force = false)
        {
            if (currentCourse is null || string.IsNullOrWhiteSpace(_userId))
                return;

            var docs = await ApiService.CallApiAsync<IEnumerable<ProjDocumentDto>>(
                $"api/documents/by-module/{moduleId}");

            var approvedActivityIds = (docs ?? Enumerable.Empty<ProjDocumentDto>())
                .Where(d =>
                    d.IsSubmission &&
                    d.ActivityId.HasValue &&
                    string.Equals(d.StudentId ?? "", _userId, StringComparison.OrdinalIgnoreCase) &&
                    IsApprovedStatus(d.Status))
                .Select(d => d.ActivityId!.Value)
                .ToHashSet();


            if(force)
            {
                if (moduleActivities.TryGetValue(moduleId, out var acts) && acts?.Count > 0)
                {
                    var moduleActIds = acts.Select(a => a.Id).ToHashSet();
                    completedActivityIds.RemoveWhere(id => moduleActIds.Contains(id));
                }
            }

            foreach (var id in approvedActivityIds)
                completedActivityIds.Add(id);

        }

        private static bool IsApprovedStatus(string? status)
        {
            var v = (status ?? "").Trim().ToLowerInvariant();
            return v is "approved" or "godkänd";
        }

        private DateOnly? GetActivityDue(ProjActivityDto a)
        {
            object ends = a.Ends;

            return ends switch
            {
                DateOnly d when d != default => d,
                DateTime dt when dt != default => DateOnly.FromDateTime(dt),
                _ => null
            };
        }

        private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

        private (string label, string cssClass) GetActivityUiStatus(ProjActivityDto a)
        {
            if (completedActivityIds.Contains(a.Id))
                return (LABEL_DONE, "badge bg-success");

            var due = GetActivityDue(a);
            
            if (due.HasValue && Today() > due.Value)
                return (LABEL_POSTPONED, "badge bg-danger");

            return (LABEL_WAITING, "badge bg-warning text-dark");
        }


    }
}
