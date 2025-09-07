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
        var modules = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>(
            $"api/course/{course.Id}/Modules");

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

        var created = await ApiService.PostAsync<CourseDto>("api/courses", placeholder);
        if (created != null)
        {
            courses = (courses ?? new List<CourseDto>()).Prepend(created).ToList();
            selectedCourse = created;
            StateHasChanged();
        }
    }

    private async Task DeleteCourseAsync(CourseDto course)
    {
        var success = await ApiService.DeleteAsync($"api/courses/{course.Id}");
        if (success && courses != null)
        {
            courses = courses.Where(c => c.Id != course.Id).ToList(); // force new list
            if (selectedCourse?.Id == course.Id)
                selectedCourse = null;

            StateHasChanged(); // force rerender
        }
    }

    private async Task SaveCourse()
    {
        if (selectedCourse == null) return;

        var dto = new UpdateCourseDto
        {
            Name = courseEditModel.Name,
            Description = courseEditModel.Description,
            Starts = courseEditModel.Starts,
            Ends = courseEditModel.Ends
        };

        var success = await ApiService.PutAsync($"api/courses/{selectedCourse.Id}", dto);
        if (success)
            await OnCourseSelected(selectedCourse);

        editingCourseId = null;
    }

    // --- Course Editing ---
    private void StartEditCourse(CourseDto course)
    {
        editingCourseId = course.Id;
        courseEditModel = new CourseEditModel
        {
            Name = course.Name,
            Description = course.Description,
            Starts = course.Starts,
            Ends = course.Ends
        };
    }

    private void CancelEditCourse()
    {
        editingCourseId = null;
        courseEditModel = new();
    }

    // --- Modules ---
    private void HandleEditModule(ModuleDto module)
    {
        selectedModuleToEdit = module;
        expandModulesAccordion = true;

        JS.InvokeVoidAsync("bootstrap.Collapse.getOrCreateInstance",
            "#collapseModules", new { toggle = true });
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