using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
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
    }
}
