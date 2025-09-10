using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Components;

[Authorize(Roles = "Teacher")] // ✅ Require Teacher role
public partial class ManageCourses : ComponentBase
{
    [Parameter] public bool IsTeacher { get; set; }
    [Parameter] public EventCallback<ModuleDto> OnEditModuleRequested { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IApiService ApiService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    private List<CourseDto>? courses;
    private CourseDto? selectedCourse;
    [Parameter] public int? CourseId { get; set; }
    private ModuleDto? selectedModuleToEdit;
    private bool expandModulesAccordion = false;

    private ModuleStrip? moduleStripRef;
    private int? selectedModuleId;

    // --- State flags ---
    private bool isLoading = true;
    private string? error;
    private bool firstRenderDone;

    // --- Lifecycle ---
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Handle first render (auth + course loading)
        if (firstRender && !firstRenderDone)
        {
            firstRenderDone = true;
            isLoading = true;
            error = null;

            try
            {
                if (AuthStateTask is not null)
                {
                    var auth = await AuthStateTask;
                    if (!(auth.User.Identity?.IsAuthenticated ?? false))
                    {
                        error = "Du måste vara inloggad för att hantera kurser.";
                        return;
                    }
                }

                await LoadCoursesAsync();
            }
            catch (Exception ex)
            {
                error = $"Fel vid laddning av kurser: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        // Handle search bar navigation (select module after render)
        if (selectedModuleId != null && selectedCourse?.Modules != null && moduleStripRef != null)
        {
            var moduleId = selectedModuleId.Value;
            selectedModuleId = null;

            var module = selectedCourse.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module != null)
            {
                await moduleStripRef.SelectModuleAsync(module);
            }
        }
    }
    // --- Course Loading ---
    private async Task LoadCoursesAsync()
    {
        try
        {
            courses = (await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses"))?.ToList();

            if (courses?.Any() == true)
            {
                if (CourseId != null)
                {
                    var course = courses.FirstOrDefault(c => c.Id == CourseId.Value);
                    if (course != null)
                    {
                        await OnCourseSelected(course);
                        return;
                    }
                }

                // fallback: select first course
                await OnCourseSelected(courses.First());
            }
        }
        catch (Exception ex)
        {
            courses = new List<CourseDto>();
            error = $"Kunde inte ladda kurser: {ex.Message}";
        }
    }

    private async Task OnCourseSelected(CourseDto course)
    {
        var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
            $"api/course/{course.Id}/Modules?includeActivities=true");

        selectedCourse = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            Starts = course.Starts,
            Ends = course.Ends,
            Modules = modules?.ToList() ?? new()
        };

        var idx = courses!.FindIndex(c => c.Id == course.Id);
        if (idx >= 0) courses[idx] = selectedCourse;
    }

    // --- Handle Search ---
    private async Task HandleSearchResult(SearchBar.SearchResult result)
    {
        if (result.Type == "Course")
        {
            var course = courses?.FirstOrDefault(c => c.Id == result.Id);
            if (course != null)
            {
                var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                    $"api/course/{course.Id}/Modules?includeActivities=true");

                course.Modules = modules?.ToList() ?? new();
                selectedCourse = course;
                selectedModuleId = course.Modules.FirstOrDefault()?.Id;
            }
        }
        else if (result.Type == "Module")
        {
            selectedCourse = courses?.FirstOrDefault(c => c.Id == result.ParentId);

            if (selectedCourse != null)
            {
                var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                    $"api/course/{selectedCourse.Id}/Modules?includeActivities=true");

                selectedCourse.Modules = modules?.ToList() ?? new();
                selectedModuleId = result.Id;
            }
        }
        else if (result.Type == "Activity")
        {
            var module = courses?
                .SelectMany(c => c.Modules ?? new List<ModuleDto>())
                .FirstOrDefault(m => m.Id == result.ParentId);

            if (module != null)
            {
                selectedCourse = courses?.FirstOrDefault(c => c.Id == module.CourseId);

                if (selectedCourse != null)
                {
                    var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                        $"api/course/{selectedCourse.Id}/Modules?includeActivities=true");

                    selectedCourse.Modules = modules?.ToList() ?? new();
                    selectedModuleId = module.Id;
                }
            }
        }

        if (selectedCourse != null)
        {
            var elementId = $"course-{selectedCourse.Id}";
            await JS.InvokeVoidAsync("scrollCourseIntoCenter", ".navstrip", elementId);
        }

        StateHasChanged();
    }



    // --- Course CRUD ---
    private async Task AddCourseAsync()
    {
        var placeholder = new CreateCourseDto
        {
            Name = "New Course",
            Description = "Placeholder description",
            Starts = DateOnly.FromDateTime(DateTime.Today),
            Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        };

        try
        {
            var created = await ApiService.PostAsync<CourseDto>("api/courses", placeholder);
            if (created != null)
            {
                var newModule = new ModuleCreateDto { Name = "New Module", Description = "New Descritption", Starts = DateOnly.FromDateTime(DateTime.Today), Ends = DateOnly.FromDateTime(DateTime.Today)};
                var module = await ApiService.PostAsync<ModuleDto>(
                    $"api/course/{created.Id}/Modules", newModule);

                if (module != null)
                    created.Modules = new List<ModuleDto> { module };

                courses ??= new List<CourseDto>();
                courses.Insert(0, created);
                selectedCourse = created;

                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            error = $"Fel vid skapande av kurs: {ex.Message}";
        }
    }

    private async Task DeleteCourseAsync(CourseDto course)
    {
        try
        {
            var success = await ApiService.DeleteAsync($"api/courses/{course.Id}");
            if (success && courses != null)
            {
                courses = courses.Where(c => c.Id != course.Id).ToList();
                if (selectedCourse?.Id == course.Id)
                    selectedCourse = null;

                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            error = $"Fel vid borttagning av kurs: {ex.Message}";
        }
    }

    private async Task SaveCourseAsync(CourseDto course)
    {
        var dto = new UpdateCourseDto
        {
            Name = course.Name,
            Description = course.Description,
            Starts = course.Starts,
            Ends = course.Ends
        };

        try
        {
            var success = await ApiService.PutAsync($"api/courses/{course.Id}", dto);
            if (success)
            {
                var idx = courses!.FindIndex(c => c.Id == course.Id);
                if (idx >= 0)
                    courses[idx] = course;

                selectedCourse = course;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            error = $"Fel vid sparande av kurs: {ex.Message}";
        }
    }

    // --- Module Updates ---
    private async Task HandleModuleUpdated(ModuleDto updatedModule)
    {
        var idx = selectedCourse?.Modules?.FindIndex(m => m.Id == updatedModule.Id) ?? -1;
        if (idx >= 0 && selectedCourse?.Modules != null)
            selectedCourse.Modules[idx] = updatedModule;

        StateHasChanged();
    }

    private async Task HandleModuleDeleted(int moduleId)
    {
        if (selectedCourse?.Modules != null)
            selectedCourse.Modules = selectedCourse.Modules.Where(m => m.Id != moduleId).ToList();

        selectedModuleToEdit = null;
        StateHasChanged();
    }

    // --- Modules ---
    private async void HandleEditModule(ModuleDto module)
    {
        selectedModuleToEdit = module;
        expandModulesAccordion = true;

        await JS.InvokeVoidAsync("eval", @"
        var el = document.querySelector('#collapseModules');
        if (el) {
            var inst = bootstrap.Collapse.getOrCreateInstance(el);
            inst.show();
        }");
    }

    private async Task HandleAddModule()
    {
        if (selectedCourse == null) return;

        var newModule = new ModuleCreateDto
        {
            Name = "New Module",
            Description = "",
            Starts = DateOnly.FromDateTime(DateTime.Today),
            Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        };

        try
        {
            var created = await ApiService.PostAsync<ModuleDto>(
                $"api/course/{selectedCourse.Id}/Modules", newModule);

            if (created != null)
            {
                selectedCourse.Modules ??= new List<ModuleDto>();
                selectedCourse.Modules.Insert(0, created);

                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            error = $"Fel vid skapande av modul: {ex.Message}";
        }
    }
}