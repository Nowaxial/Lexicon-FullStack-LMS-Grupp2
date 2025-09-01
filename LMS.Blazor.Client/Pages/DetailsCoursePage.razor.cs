using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Collections.Generic;

namespace LMS.Blazor.Client.Pages
{
    public partial class DetailsCoursePage
    {
        [Inject] private IApiService ApiService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [Parameter] public int id { get; set; }   

        private CourseDto? course;

        private List<ModuleDto>? modules = new();

        private readonly Dictionary<int, List<ProjActivityDto>> activitiesByModuleId = new();

        private bool isLoading;
        private bool firstRenderDone;
        private string? loadError;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRenderDone)
            {
                firstRenderDone = true;
                isLoading = true; 
                StateHasChanged();

                try
                {
                    await LoadCourseAsync(id);
                    await LoadModulesAsync(id);
                    await LoadActivitiesForModulesAsync(modules);

                }
                catch (Exception ex) 
                { 
                    loadError = "Fel vid hämtning av kurs: " + ex.Message; 
                }
                finally 
                { 
                    isLoading = false; 
                    StateHasChanged(); 
                }
            }
        }

        private async Task LoadCourseAsync(int courseId)
        {
            course = await ApiService.CallApiAsync<CourseDto>($"api/courses/{id}?includeModules=false&includeActivities=false");
            if (course is null)
                throw new InvalidOperationException("Kursen kunde inte hittas.");
        }

        private async Task LoadModulesAsync(int courseId)
        {
            var list = await ApiService.CallApiAsync<IEnumerable<ModuleDto>>($"api/course/{courseId}/modules?includeActivities=false");

            modules = list?.ToList() ?? new();
        }

        private async Task LoadActivitiesForModulesAsync(IEnumerable<ModuleDto> targetModules)
        {
            activitiesByModuleId.Clear();
            if (!targetModules.Any()) return;

            var tasks = targetModules.Select(async m =>
            {
                var acts = await ApiService.CallApiAsync<IEnumerable<ProjActivityDto>>(
                    $"api/modules/{m.Id}/activities");

                activitiesByModuleId[m.Id] = acts?
                    .OrderBy(a => a.Starts)
                    .ToList() ?? new List<ProjActivityDto>();
            });

            await Task.WhenAll(tasks);
        }

        private IReadOnlyList<ProjActivityDto> ActivitiesFor(int moduleId) =>
            activitiesByModuleId.TryGetValue(moduleId, out var list)
                ? list
                : Array.Empty<ProjActivityDto>();

        private string ModuleCollapseId(int moduleId) => $"mod-{moduleId}-acts";
        private string ActivityCollapseId(int moduleId, int activityId) => $"mod-{moduleId}-act-{activityId}";
    }
}
