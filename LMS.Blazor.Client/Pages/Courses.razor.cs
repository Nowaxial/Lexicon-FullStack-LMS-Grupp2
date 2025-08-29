using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.Common;
using LMS.Shared.DTOs.EntitiesDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Pages
{
    public partial class Courses
    {
        [Inject] private IApiService ApiService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private List<CourseDto>? courses;
        private CourseDto? selectedCourse;

        private CreateCourseDto newCourse = new()
        {
            Starts = DateOnly.FromDateTime(DateTime.Today),
            Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        };

        // Users state
        private List<UserDto>? users;
        private bool usersLoaded = false;
        private bool isLoadingUsers = false;
        private string? usersError;

        private bool firstRenderDone;
        private bool isLoading;

        private bool isAssigningUser;
        private string? assignError;
        private string? assignSuccess;


        private string userFilter = string.Empty;

        private IEnumerable<UserDto> FilteredUsers
            => string.IsNullOrWhiteSpace(userFilter)
                ? (users ?? Enumerable.Empty<UserDto>())
                : (users ?? Enumerable.Empty<UserDto>())
                    .Where(u =>
                        StartsWithIgnoreCase(u.FullName, userFilter) ||
                        StartsWithIgnoreCase(u.UserName, userFilter) ||
                        StartsWithIgnoreCase(u.Email, userFilter))
                    .OrderBy(u => u.FullName ?? u.UserName ?? u.Email);

        private static bool StartsWithIgnoreCase(string? value, string prefix)
            => !string.IsNullOrWhiteSpace(value)
               && !string.IsNullOrWhiteSpace(prefix)
               && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        // Load users when the Assign User collapse is first opened
        private async Task LoadUsersIfNeeded()
        {
            if (usersLoaded || isLoadingUsers) return;
            await ReloadUsersAsync();
        }

        // Refresh users
        private async Task ReloadUsersAsync()
        {
            try
            {
                isLoadingUsers = true;
                usersError = null;

                // If your API is paged, tweak as needed; here we fetch up to 200 and filter client-side
                var page = await ApiService.CallApiAsync<PagedResult<UserDto>>("api/users?size=200");
                users = page?.Items?.ToList() ?? new List<UserDto>();

                usersLoaded = true;
            }
            catch (Exception ex)
            {
                usersError = "Failed to load users: " + ex.Message;
            }
            finally
            {
                isLoadingUsers = false;
                StateHasChanged();
            }
        }

        private async Task CallAPIAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.IsInRole("Teacher"))
            {
                courses = (await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses"))?.ToList();
            }
            else if (user.IsInRole("Student"))
            {
                courses = (await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses/my"))?.ToList();
            }
            else
            {
                courses = new();
            }
        }

        private async Task AddCourseAsync()
        {
            var created = await ApiService.PostAsync<CourseDto>("api/courses", newCourse);
            if (created != null)
            {
                courses ??= new List<CourseDto>();
                courses.Add(created);

                // Reset form
                newCourse = new CreateCourseDto
                {
                    Starts = DateOnly.FromDateTime(DateTime.Today),
                    Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
                };

                // Close "Add Course" collapse via Bootstrap JS
                await JS.InvokeVoidAsync(
                     "eval",
                     "bootstrap.Collapse.getOrCreateInstance(document.getElementById('addCourseCollapse')).hide()"
                );
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

        private void SelectCourse(CourseDto course)
        {
            selectedCourse = course;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRenderDone)
            {
                firstRenderDone = true;
                isLoading = true;
                await CallAPIAsync();
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task AssignUserToSelectedCourse(UserDto u)
        {
            assignError = null;
            assignSuccess = null;

            if (selectedCourse is null)
            {
                assignError = "Pick a course first.";
                return;
            }

            // Requires UserDto to have Id (string). If your DTO lacks Id, expose it.
            if (string.IsNullOrWhiteSpace(u.Id))
            {
                assignError = "Cannot assign: user id is missing.";
                return;
            }

            isAssigningUser = true;
            try
            {
                // Use a method that DOESN'T deserialize (or use HttpClient directly)
                await ApiService.PostAsync<object>(
                    $"api/courses/{selectedCourse.Id}/users/{u.Id}", new { });
                assignSuccess = $"Assigned {u.FullName ?? u.UserName ?? u.Email} to {selectedCourse.Name}.";
            }

            catch (Exception ex)
            {
                assignError = "Failed to assign: " + ex.Message;
            }
            finally
            {
                isAssigningUser = false;
                StateHasChanged();
            }
        }
    }
}