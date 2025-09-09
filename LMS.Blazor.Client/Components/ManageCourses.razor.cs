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

    private int? editingCourseId;
    private CourseEditModel courseEditModel = new();

    private ModuleDto? selectedModuleToEdit;
    private bool expandModulesAccordion = false;

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
        // ✅ load modules *with activities*
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

        // 1. Create the course
        var created = await ApiService.PostAsync<CourseDto>("api/courses", placeholder);
        if (created != null)
        {
            // 2. Create an empty module for it
            var newModule = new ModuleCreateDto
            {
                Name = "New Module",
                Description = string.Empty
            };

            var module = await ApiService.PostAsync<ModuleDto>(
                $"api/course/{created.Id}/Modules", newModule);

            if (module != null)
                created.Modules = new List<ModuleDto> { module };

            // 3. Add to local state
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
        // Attach the course reference so ManageModules has access to dates
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

    // --- Helper model ---
    private class CourseEditModel
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }
    }
}