using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

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

    // unified activity form
    private int? editingActivityId; // null = add mode, value = editing
    private ActivityEditModel activityFormModel = new();

    private string activityFilter = string.Empty;
    private bool isLoading = false;
    private string? errorMessage;

    protected override Task OnParametersSetAsync()
    {
        activeModule = SelectedModule;

        if (activeModule != null)
        {
            moduleEditModel = new ModuleEditModel
            {
                Name = activeModule.Name,
                Description = activeModule.Description,
                Starts = activeModule.Starts,
                Ends = activeModule.Ends,

                CourseStarts = activeModule.Course?.Starts ?? DateOnly.MinValue,
                CourseEnds = activeModule.Course?.Ends ?? DateOnly.MaxValue
            };
        }

        editingModuleId = null;
        ResetActivityForm();
        return Task.CompletedTask;
    }

    private IEnumerable<ProjActivityDto>? FilteredActivities =>
        activeModule?.Activities?
            .Where(a => string.IsNullOrWhiteSpace(activityFilter) ||
                        (a.Title ?? "").Contains(activityFilter, StringComparison.OrdinalIgnoreCase));

    // ----------------- ACTIVITY CRUD -----------------

    private async Task HandleActivitySubmit()
    {
        if (editingActivityId == null)
            await SaveNewActivity();
        else
            await SaveActivity(editingActivityId.Value);
    }

    private async Task SaveNewActivity()
    {
        if (activeModule == null) return;

        var dto = new CreateProjActivityDto
        {
            Title = activityFormModel.Title,
            Type = activityFormModel.Type,
            Starts = activityFormModel.Starts,
            Ends = activityFormModel.Ends
        };

        var created = await ApiService.PostAsync<ProjActivityDto>(
            $"api/modules/{activeModule.Id}/activities", dto);

        if (created != null)
        {
            // rebuild module with updated activities
            activeModule = activeModule with
            {
                Activities = (activeModule.Activities ?? new List<ProjActivityDto>())
                    .Append(created)
                    .ToList()
            };

            if (OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(activeModule);
        }

        ResetActivityForm();
    }

    private async Task SaveActivity(int id)
    {
        if (activeModule is null) return;

        var dto = new UpdateProjActivityDto
        {
            Title = activityFormModel.Title,
            Type = activityFormModel.Type,
            Starts = activityFormModel.Starts,
            Ends = activityFormModel.Ends
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
                    // ✅ Replace in-place to persist
                    activeModule.Activities[idx] = updated;
                }
            }

            if (OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(activeModule);
        }

        ResetActivityForm();
    }

    private void StartEditActivity(ProjActivityDto activity)
    {
        editingActivityId = activity.Id;
        activityFormModel = new ActivityEditModel
        {
            Title = activity.Title,
            Type = activity.Type,
            Starts = activity.Starts,
            Ends = activity.Ends,
            ModuleStarts = activeModule?.Starts.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
            ModuleEnds = activeModule?.Ends.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue
        };
    }

    private void ResetActivityForm()
    {
        editingActivityId = null;
        activityFormModel = new ActivityEditModel
        {
            ModuleStarts = activeModule?.Starts.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
            ModuleEnds = activeModule?.Ends.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue
        };
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
                activeModule.Activities.RemoveAt(idx);

            if (OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(activeModule);
        }

        ResetActivityForm();
    }

    // ----------------- MODULE CRUD -----------------

    private void StartEditModule(ModuleDto module)
    {
        editingModuleId = module.Id;
        moduleEditModel = new ModuleEditModel
        {
            Name = module.Name,
            Description = module.Description,
            Starts = module.Starts,
            Ends = module.Ends,
            CourseStarts = module.Course?.Starts ?? DateOnly.MinValue,
            CourseEnds = module.Course?.Ends ?? DateOnly.MaxValue
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
            var updated = await ApiService.CallApiAsync<ModuleDto>(
                $"api/course/{activeModule.CourseId}/Modules/{activeModule.Id}?includeActivities=true");

            if (updated != null && OnModuleUpdated.HasDelegate)
                await OnModuleUpdated.InvokeAsync(updated);
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
            activeModule = null;

            if (OnModuleDeleted.HasDelegate)
                await OnModuleDeleted.InvokeAsync(deletedId);
        }
    }

    // ----------------- ViewModels -----------------

    private class ModuleEditModel : IValidatableObject
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required, StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateOnly Starts { get; set; }

        [Required]
        public DateOnly Ends { get; set; }

        public DateOnly CourseStarts { get; set; }
        public DateOnly CourseEnds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Starts > Ends)
                yield return new ValidationResult("Module start must be before end", new[] { nameof(Starts), nameof(Ends) });

            if (Starts < CourseStarts || Ends > CourseEnds)
                yield return new ValidationResult($"Module dates must fit within course {CourseStarts:d} – {CourseEnds:d}",
                    new[] { nameof(Starts), nameof(Ends) });
        }
    }

    private class ActivityEditModel : IValidatableObject
    {
        [Required, StringLength(100)]
        public string Title { get; set; } = "";

        [Required, StringLength(50)]
        public string Type { get; set; } = "";

        [Required]
        public DateTime Starts { get; set; } = DateTime.Today;

        [Required]
        public DateTime Ends { get; set; } = DateTime.Today.AddDays(1);

        public DateTime ModuleStarts { get; set; }
        public DateTime ModuleEnds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Starts > Ends)
                yield return new ValidationResult("Activity start must be before end", new[] { nameof(Starts), nameof(Ends) });

            if (Starts < ModuleStarts || Ends > ModuleEnds)
                yield return new ValidationResult($"Activity dates must fit within module {ModuleStarts:g} – {ModuleEnds:g}",
                    new[] { nameof(Starts), nameof(Ends) });
        }
    }
}