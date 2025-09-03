using LMS.Shared.DTOs.EntitiesDtos;
using System.Globalization;

namespace LMS.Blazor.Components.Pages
{
    public partial class Home
    {
        private readonly CultureInfo sv = CultureInfo.GetCultureInfo("sv-SE");
        private List<CourseDto> courses = new();
        private bool isLoading;
        private string? error;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            try
            {
                var client = HttpClientFactory.CreateClient("LmsAPIClient");
                var data = await client.GetFromJsonAsync<IEnumerable<CourseDto>>("api/courses");

                // EXACTLY THREE LATEST (by start date). Change the key if you prefer CreatedAt.
                courses = data?
                    .OrderByDescending(c => c.Starts)   // latest first
                    .Take(3)
                    .ToList() ?? new();
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }

        private string Duration(DateOnly start, DateOnly end)
        {
            var s = start.ToDateTime(TimeOnly.MinValue);
            var e = end.ToDateTime(TimeOnly.MinValue);

            if (e < s) return "-";

            var days = (int)Math.Ceiling((e - s).TotalDays) + 1; // include end day
            if (days < 7)
                return days == 1 ? "1 dag" : $"{days} dagar";

            var weeks = days / 7;
            //var rem = days % 7;
            return $"{weeks} veckor";
        }
    }
}