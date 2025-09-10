using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Components;

public partial class ManageCourses : ComponentBase
{
    [Parameter] public bool IsTeacher { get; set; }
    [Parameter] public EventCallback<ModuleDto> OnEditModuleRequested { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IApiService ApiService { get; set; } = default!;

    private List<CourseDto>? courses;
    private CourseDto? selectedCourse;

    private ModuleDto? selectedModuleToEdit;
    private bool expandModulesAccordion = false;

    private ModuleStrip? moduleStripRef;
    private int? selectedModuleId;

    // --- Lifecycle ---
    protected override async Task OnInitializedAsync() => await LoadCoursesAsync();

    // --- Course Loading ---
    private async Task LoadCoursesAsync()
    {
        try
        {
            courses = (await ApiService.CallApiAsync<IEnumerable<CourseDto>>("api/courses"))?.ToList();

            if (courses?.Any() == true)
                await OnCourseSelected(courses.First());
        }
        catch (Exception ex)
        {
            courses = new List<CourseDto>();
            Console.WriteLine($"Failed to load courses: {ex.Message}");
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
                // reload modules with activities
                var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
                    $"api/course/{course.Id}/Modules?includeActivities=true");

                course.Modules = modules?.ToList() ?? new();
                selectedCourse = course;

                // auto-select first module
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

        // scroll NavStrip to selected course
        if (selectedCourse != null)
        {
            var elementId = $"course-{selectedCourse.Id}";
            await JS.InvokeVoidAsync("scrollCourseIntoCenter", ".navstrip", elementId);
        }

        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (selectedModuleId != null && selectedCourse?.Modules != null && moduleStripRef != null)
        {
            var moduleId = selectedModuleId.Value;
            selectedModuleId = null; // ✅ clear first to break any loop

            var module = selectedCourse.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module != null)
            {
                // don’t force StateHasChanged here, only scroll
                await moduleStripRef.SelectModuleAsync(module);
            }
        }
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

        var created = await ApiService.PostAsync<CourseDto>("api/courses", placeholder);
        if (created != null)
        {
            var newModule = new ModuleCreateDto
            {
                Name = "New Module",
                Description = string.Empty
            };

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

    private async Task DeleteCourseAsync(CourseDto course)
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

    private async Task SaveCourseAsync(CourseDto course)
    {
        var dto = new UpdateCourseDto
        {
            Name = course.Name,
            Description = course.Description,
            Starts = course.Starts,
            Ends = course.Ends
        };

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

    // --- Module Updates ---
    private async Task HandleModuleUpdated(ModuleDto updatedModule)
    {
        if (selectedCourse?.Modules == null) return;

        updatedModule = updatedModule with { Course = selectedCourse };

        var idx = selectedCourse.Modules.FindIndex(m => m.Id == updatedModule.Id);
        if (idx >= 0)
        {
            selectedCourse.Modules[idx] = updatedModule;
        }

        selectedModuleToEdit = updatedModule;
        StateHasChanged();
    }

    private async Task HandleModuleDeleted(int moduleId)
    {
        if (selectedCourse is null) return;

        var updatedModules = selectedCourse.Modules?
            .Where(m => m.Id != moduleId)
            .ToList() ?? new List<ModuleDto>();

        selectedCourse = new CourseDto
        {
            Id = selectedCourse.Id,
            Name = selectedCourse.Name,
            Description = selectedCourse.Description,
            Starts = selectedCourse.Starts,
            Ends = selectedCourse.Ends,
            Modules = updatedModules
        };

        courses = courses?
            .Select(c => c.Id == selectedCourse.Id ? selectedCourse : c)
            .ToList();

        selectedModuleToEdit = null;
        StateHasChanged();
    }

    // --- Modules ---
    private async void HandleEditModule(ModuleDto module)
    {
        selectedModuleToEdit = module with { Course = selectedCourse };
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

        var created = await ApiService.PostAsync<ModuleDto>(
            $"api/course/{selectedCourse.Id}/Modules", newModule);

        if (created != null)
        {
            selectedCourse.Modules ??= new List<ModuleDto>();
            selectedCourse.Modules.Insert(0, created);

            StateHasChanged();
        }
    }
}