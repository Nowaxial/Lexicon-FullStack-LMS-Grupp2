using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LMS.Blazor.Client.Components;

public partial class ManageModules : ComponentBase
{
    [Inject] private IApiService ApiService { get; set; } = default!;
    [Parameter] public ModuleDto? SelectedModule { get; set; }

    private ModuleDto? activeModule;
    private int? editingModuleId;
    private ModuleEditModel moduleEditModel = new();

    private int? editingActivityId;
    private ActivityEditModel activityEditModel = new();
    private ActivityEditModel addActivityModel = new();

    private string activityFilter = string.Empty;
    private bool isLoading = false;
    private string? errorMessage;

    /// <summary>
    /// Whenever a new SelectedModule is pushed in from outside (ModuleStrip → ManageCourses),
    /// load its full details and open it in edit mode.
    /// </summary>
    protected override Task OnParametersSetAsync()
    {
        if (SelectedModule != null)
        {
            activeModule = SelectedModule;
            editingModuleId = activeModule.Id;
            moduleEditModel = new ModuleEditModel
            {
                Name = activeModule.Name,
                Description = activeModule.Description,
                Starts = activeModule.Starts,
                Ends = activeModule.Ends
            };
        }
        else
        {
            activeModule = null;
            editingModuleId = null;
        }

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
            $"api/modules-library/{activeModule.Id}/activities", dto);

        if (created != null)
        {
            if (activeModule.Activities != null)
            {
                activeModule.Activities.Add(created);
            }
            else
            {
                // Create a *new* ModuleDto copy with initialized Activities
                activeModule = new ModuleDto
                {
                    Id = activeModule.Id,
                    Name = activeModule.Name,
                    Description = activeModule.Description,
                    Starts = activeModule.Starts,
                    Ends = activeModule.Ends,
                    Activities = new List<ProjActivityDto> { created }
                };
            }
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

        var success = await ApiService.PutAsync($"api/activities/{id}", dto);
        if (success)
        {
            var idx = activeModule.Activities?.FindIndex(a => a.Id == id) ?? -1;
            if (idx >= 0)
            {
                activeModule!.Activities![idx] = new ProjActivityDto
                {
                    Id = id,
                    Title = dto.Title,
                    Type = dto.Type,
                    Starts = dto.Starts,
                    Ends = dto.Ends
                };
            }
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
        if (await ApiService.DeleteAsync($"api/activities/{id}"))
            activeModule?.Activities?.RemoveAll(a => a.Id == id);
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
            Name = moduleEditModel.Name,
            Description = moduleEditModel.Description,
            Starts = moduleEditModel.Starts,
            Ends = moduleEditModel.Ends
        };

        var success = await ApiService.PutAsync($"api/modules-library/{activeModule.Id}", dto);
        if (success)
        {
            activeModule = await ApiService.CallApiAsync<ModuleDto>(
                $"api/modules-library/{activeModule.Id}?includeActivities=true");
        }

        editingModuleId = null;
    }

    private void CancelEditModule() => editingModuleId = null;

    private async Task DeleteModule(int id)
    {
        if (await ApiService.DeleteAsync($"api/modules-library/{id}"))
        {
            if (activeModule?.Id == id) activeModule = null;
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