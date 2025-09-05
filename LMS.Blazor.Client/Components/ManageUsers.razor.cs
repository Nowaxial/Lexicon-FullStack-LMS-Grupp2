using LMS.Shared.DTOs.Common;
using LMS.Shared.DTOs.UsersDtos;
using LMS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LMS.Blazor.Client.Components; // 👈 update to match your project

public partial class ManageUsers : ComponentBase
{
    [Inject] private IApiService ApiService { get; set; } = default!;

    private List<UserDto>? users;
    private bool isLoading = true;
    private string? errorMessage;
    private string userFilter = string.Empty;

    private string? editingUserId;
    private UserEditModel userEditModel = new();

    private UserEditModel addUserModel = new();

    private IEnumerable<UserDto> FilteredUsers =>
        string.IsNullOrWhiteSpace(userFilter)
            ? (users ?? Enumerable.Empty<UserDto>())
            : (users ?? Enumerable.Empty<UserDto>())
                .Where(u =>
                    StartsWithIgnoreCase(u.FullName, userFilter) ||
                    StartsWithIgnoreCase(u.UserName, userFilter) ||
                    StartsWithIgnoreCase(u.Email, userFilter))
                .OrderBy(u => u.FullName ?? u.UserName ?? u.Email);

    private static bool StartsWithIgnoreCase(string? value, string prefix) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.IsNullOrWhiteSpace(prefix) &&
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync() => await LoadUsers();

    private async Task LoadUsers()
    {
        try
        {
            isLoading = true;
            var page = await ApiService.CallApiAsync<PagedResult<UserDto>>("api/users?size=50");
            users = page?.Items?.ToList() ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load users: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveNewUser()
    {
        var dto = new CreateUserDto
        {
            UserName = addUserModel.UserName,
            Email = addUserModel.Email,
            Password = addUserModel.Password,
            Roles = new List<string> { addUserModel.Role }
        };

        var created = await ApiService.PostAsync<UserDto>("api/users", dto);
        if (created != null)
        {
            users ??= new();
            users.Add(created);
        }

        ResetAddForm();
    }

    private void ResetAddForm() => addUserModel = new UserEditModel();

    private void StartEditUser(UserDto user)
    {
        editingUserId = user.Id;
        userEditModel = new UserEditModel
        {
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            Role = user.Roles?.FirstOrDefault() ?? "Student"
        };
    }

    private async Task SaveUser(string id)
    {
        var dto = new UpdateUserDto
        {
            UserName = userEditModel.UserName,
            Email = userEditModel.Email
        };

        var success = await ApiService.PutAsync($"api/users/{id}", dto);
        if (success)
        {
            var existing = users?.FirstOrDefault(u => u.Id == id);
            if (existing != null)
            {
                var updated = new UserDto
                {
                    Id = existing.Id,
                    FullName = existing.FullName,
                    UserName = userEditModel.UserName,
                    Email = userEditModel.Email,
                    Roles = new List<string> { userEditModel.Role }
                };

                users![users.IndexOf(existing)] = updated;
            }
        }

        CancelEdit();
    }

    private void CancelEdit()
    {
        editingUserId = null;
        userEditModel = new();
    }

    private async Task DeleteUser(string id)
    {
        if (await ApiService.DeleteAsync($"api/users/{id}"))
            users?.RemoveAll(u => u.Id == id);
    }

    private bool IsTeacher(UserDto user) =>
        user.Roles?.Contains("Teacher") == true;

    private class UserEditModel
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Student";
    }
}