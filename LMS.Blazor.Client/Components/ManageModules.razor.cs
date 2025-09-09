using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LMS.Blazor.Client.Components;

public partial class ManageModules : ComponentBase
{
    [Inject] private IApiService ApiService { get; set; } = default!;
    [Parameter] public ModuleDto? SelectedModule { get; set; }
    [Parameter] public EventCallback<ModuleDto> OnModuleUpdated { get; set; }
    [Parameter] public EventCallback<int> OnModuleDeleted { get; set; }

    private ModuleDto? activeModule;
    private int? editingModuleId;
    private ModuleEditModel moduleEditModel = new();

    private int? editingActivityId;
    private ActivityEditModel activityEditModel = new();
    private ActivityEditModel addActivityModel = new();

    private string activityFilter = string.Empty;
    private bool isLoading = false;
    private string? errorMessage;

    protected override Task OnParametersSetAsync()
    {
        // ✅ Always re-sync with parent
        activeModule = SelectedModule;

        if (activeModule != null)
        {
            moduleEditModel = new ModuleEditModel
            {
                Name = activeModule.Name,
                Description = activeModule.Description,
                Starts = activeModule.Starts,
                Ends = activeModule.Ends
            };
        }

        editingModuleId = null;
        return Task.CompletedTask;
    }

    private IEnumerable<ProjActivityDto>? FilteredActivities =>
        activeModule?.Activities?
            .Where(a => string.IsNullOrWhiteSpace(activityFilter) ||
                        (a.Title ?? "").Contains(activityFilter, StringComparison.OrdinalIgnoreCase));

    // --- Add activity ---
    private async Task SaveNewActivity()
    {
        if (activeModule == null) return;

        var dto = new CreateProjActivityDto
        {
            Title = addActivityModel.Title,
            Type = addActivityModel.Type,
            Starts = addActivityModel.Starts,
            Ends = addActivityModel.Ends
        };

        var created = await ApiService.PostAsync<ProjActivityDto>(
            $"api/modules/{activeModule.Id}/activities", dto);

        if (created != null)
        {
            if (activeModule.Activities != null)
            {
                activeModule.Activities.Add(created);
            }
            else
            {
                activeModule = new ModuleDto
                {
                    Id = activeModule.Id,
                    CourseId = activeModule.CourseId,
                    Name = activeModule.Name,
                    Description = activeModule.Description,
                    Starts = activeModule.Starts,
                    Ends = activeModule.Ends,
                    Activities = new List<ProjActivityDto> { created }
                };
            }

            if (OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(activeModule);
        }

        ResetAddActivityForm();
    }

    private void ResetAddActivityForm() => addActivityModel = new();

    // --- Edit activity ---
    private void StartEditActivity(ProjActivityDto activity)
    {
        editingActivityId = activity.Id;
        activityEditModel = new ActivityEditModel
        {
            Title = activity.Title,
            Type = activity.Type,
            Starts = activity.Starts,
            Ends = activity.Ends
        };
    }

    private async Task SaveActivity(int id)
    {
        if (activeModule is null) return;

        var dto = new UpdateProjActivityDto
        {
            Title = activityEditModel.Title,
            Type = activityEditModel.Type,
            Starts = activityEditModel.Starts,
            Ends = activityEditModel.Ends
        };

        var success = await ApiService.PutAsync(
            $"api/modules/{activeModule.Id}/activities/{id}", dto);

        if (success && activeModule.Activities != null)
        {
            var updated = await ApiService.CallApiAsync<ProjActivityDto>(
                $"api/modules/{activeModule.Id}/activities/{id}");

            if (updated != null)
            {
                var idx = activeModule.Activities.FindIndex(a => a.Id == id);
                if (idx >= 0)
                {
                    // ✅ Replace in-place, like Add
                    activeModule.Activities[idx] = updated;
                }
            }

            if (OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(activeModule);

            StateHasChanged();
        }

        editingActivityId = null;
        activityEditModel = new();
    }

    private void CancelEditActivity()
    {
        editingActivityId = null;
        activityEditModel = new();
    }

    private async Task DeleteActivity(int id)
    {
        if (activeModule is null) return;

        var success = await ApiService.DeleteAsync(
            $"api/modules/{activeModule.Id}/activities/{id}");

        if (success && activeModule.Activities != null)
        {
            var idx = activeModule.Activities.FindIndex(a => a.Id == id);
            if (idx >= 0)
            {
                // ✅ Remove in-place
                activeModule.Activities.RemoveAt(idx);
            }

            if (OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(activeModule);

            StateHasChanged();
        }
    }


    // --- Module editing ---
    private void StartEditModule(ModuleDto module)
    {
        editingModuleId = module.Id;
        moduleEditModel = new ModuleEditModel
        {
            Name = module.Name,
            Description = module.Description,
            Starts = module.Starts,
            Ends = module.Ends
        };
    }

    private async Task SaveModule()
    {
        if (activeModule is null) return;

        var dto = new ModuleUpdateDto
        {
            Id = activeModule.Id,
            Name = moduleEditModel.Name,
            Description = moduleEditModel.Description,
            Starts = moduleEditModel.Starts,
            Ends = moduleEditModel.Ends,
            CourseId = activeModule.CourseId
        };

        var success = await ApiService.PutAsync(
            $"api/course/{activeModule.CourseId}/Modules/{activeModule.Id}", dto);

        if (success)
        {
            // ✅ Re-fetch updated module from API
            var updated = await ApiService.CallApiAsync<ModuleDto>(
                $"api/course/{activeModule.CourseId}/Modules/{activeModule.Id}?includeActivities=true");

            if (updated != null && OnModuleUpdated.HasDelegate)
            {
                await OnModuleUpdated.InvokeAsync(updated);
            }
        }

        editingModuleId = null;
    }

    private void CancelEditModule() => editingModuleId = null;

    private async Task DeleteModule(int id)
    {
        if (activeModule is null) return;

        var success = await ApiService.DeleteAsync(
            $"api/course/{activeModule.CourseId}/Modules/{id}");

        if (success)
        {
            var deletedId = activeModule.Id;

            // ✅ Clear local module reference
            activeModule = null;

            if (OnModuleDeleted.HasDelegate)
                await OnModuleDeleted.InvokeAsync(deletedId);

            StateHasChanged();
        }
    }

    // --- Helper models ---
    private class ModuleEditModel
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }
    }

    private class ActivityEditModel
    {
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime Starts { get; set; } = DateTime.Today;
        public DateTime Ends { get; set; } = DateTime.Today.AddDays(1);
    }
}