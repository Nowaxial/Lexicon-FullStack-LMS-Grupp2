//using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
//using Microsoft.AspNetCore.Components;
//using Microsoft.JSInterop;
//using LMS.Blazor.Client.Services;

//namespace LMS.Blazor.Client.Components;

//public partial class ModuleStrip : ComponentBase
//{
//    [Parameter] public IEnumerable<ModuleDto>? Modules { get; set; }
//    [Parameter] public string Id { get; set; } = "modulestrip";
//    [Parameter] public bool IsTeacher { get; set; } = false;
//    [Parameter] public int CourseId { get; set; }
//    [Parameter] public EventCallback<ModuleDto> OnEditModuleRequested { get; set; }

//    private List<ModuleDto> EditableModules = new();
//    private ElementReference modulesStripRef;
//    private int ActiveIndex = 0;
//    private bool AddingNewModule = false;

//    [Inject] private IJSRuntime JS { get; set; } = default!;
//    [Inject] private IApiService Api { get; set; } = default!;

//    protected override void OnParametersSet()
//    {
//        EditableModules = Modules?.ToList() ?? new();
//    }

//    private string LeftIcon =>
//        AddingNewModule ? "bi bi-check-lg text-success" :
//        (IsTeacher && ActiveIndex == 0 ? "bi bi-plus-lg text-success" : "bi bi-chevron-left");

//    private string RightIcon =>
//        AddingNewModule ? "bi bi-x-lg text-muted" : "bi bi-chevron-right";

//    private bool IsLeftDisabled => !AddingNewModule && ActiveIndex == 0 && !IsTeacher;
//    private bool IsRightDisabled => !AddingNewModule && ActiveIndex == EditableModules.Count - 1;

//    private async Task OnLeftClick()
//    {
//        if (AddingNewModule)
//        {
//            AddingNewModule = false;

//            var draft = EditableModules[ActiveIndex];

//            var dto = new ModuleCreateDto
//            {
//                Name = draft.Name ?? "New Module",
//                Description = draft.Description ?? "",
//                Starts = DateOnly.FromDateTime(DateTime.Today),
//                Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
//            };

//            // Save to backend
//            if (Modules == null || !Modules.Any())
//                return;

//            var created = await Api.PostAsync<ModuleDto>($"api/course/{CourseId}/Modules", dto);
//            if (created != null)
//            {
//                EditableModules[ActiveIndex] = created;
//                var modulesList = Modules?.ToList() ?? new List<ModuleDto>();
//                modulesList.Insert(0, created);

//                // If parent should know, raise event or update binding
//                await OnEditModule(created);
//            }
//        }
//        else if (IsTeacher && ActiveIndex == 0)
//        {
//            // ➕ Add new empty module to the *left*
//            var newModule = new ModuleDto { Name = "New Module", Description = "Empty module" };
//            EditableModules.Insert(0, newModule);
//            ActiveIndex = 0;
//            AddingNewModule = true;
//        }
//        else
//        {
//            await Scroll(-1);
//        }

//        StateHasChanged();
//    }

//    private async Task OnRightClick()
//    {
//        if (AddingNewModule)
//        {
//            // ❌ Cancel
//            AddingNewModule = false;
//            EditableModules.RemoveAt(ActiveIndex);
//            ActiveIndex = Math.Clamp(ActiveIndex, 0, EditableModules.Count - 1);
//        }
//        else
//        {
//            await Scroll(1);
//        }
//        StateHasChanged();
//    }

//    private async Task Scroll(int direction)
//    {
//        if (!EditableModules.Any()) return;

//        ActiveIndex = Math.Clamp(ActiveIndex + direction, 0, EditableModules.Count - 1);
//        await JS.InvokeVoidAsync("scrollStrip", ".modules-strip", direction);
//    }

//    private async Task OnEditModule(ModuleDto module)
//    {
//        var dto = new ModuleDto
//        {
//            Name = module.Name,
//            Description = module.Description,
//            Starts = module.Starts,
//            Ends = module.Ends,
//            CourseId = module.CourseId,
//            Activities = module.Activities,
//        };
//        await OnEditModuleRequested.InvokeAsync(dto);
//    }
//}