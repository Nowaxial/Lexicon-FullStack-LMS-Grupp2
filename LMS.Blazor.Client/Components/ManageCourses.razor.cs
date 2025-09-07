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

    private CreateCourseDto newCourse = new()
    {
        Starts = DateOnly.FromDateTime(DateTime.Today),
        Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
    };

    private int? editingCourseId;
    private CourseEditModel courseEditModel = new();
    private ModuleDto? selectedModuleToEdit;
    private bool expandModulesAccordion = false;
    private void HandleEditModule(ModuleDto module)
    {
        selectedModuleToEdit = module;
        expandModulesAccordion = true;

        // manually expand accordion
        JS.InvokeVoidAsync("bootstrap.Collapse.getOrCreateInstance",
            "#collapseModules", new { toggle = true });
    }
    protected override async Task OnInitializedAsync() => await LoadCoursesAsync();

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

    private async Task AddCourseAsync()
    {
        var created = await ApiService.PostAsync<CourseDto>("api/courses", newCourse);
        if (created != null)
        {
            courses ??= new();
            courses.Add(created);

            newCourse = new CreateCourseDto
            {
                Starts = DateOnly.FromDateTime(DateTime.Today),
                Ends = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
            };
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

    // --- Helper model ---
    private class CourseEditModel
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }
    }
}