using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Pages
{
    public partial class DetailsCoursePage
    {
        [Inject] private IApiService ApiService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [Parameter] public int? courseId { get; set; } 
        [Parameter] public string? slug { get; set; } 

        private CourseDto? course;
        private bool isLoading;
        private bool firstRenderDone;
        private string? loadError;

        [Parameter] public int id { get; set; }   // ✅ only id

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRenderDone)
            {
                firstRenderDone = true;
                isLoading = true; StateHasChanged();
                try
                {
                    course = await ApiService.CallApiAsync<CourseDto>($"api/courses/{id}?includeModules=false&includeActivities=false");
                    if (course is null) loadError = "Kursen kunde inte hittas.";
                }
                catch (Exception ex) { loadError = "Fel vid hämtning av kurs: " + ex.Message; }
                finally { isLoading = false; StateHasChanged(); }
            }
        }


    }
}