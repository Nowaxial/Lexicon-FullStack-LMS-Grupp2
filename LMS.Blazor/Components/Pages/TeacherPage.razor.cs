using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LMS.Blazor.Components.Pages
{
    public partial class TeacherPage
    {
        [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }
        private string displayName = "Lärare";

        protected override async Task OnInitializedAsync()
        {
            if (AuthStateTask is not null)
            {
                var auth = await AuthStateTask;
                displayName = auth.User.Identity?.Name ?? displayName;
            }
        }

        // ---- Mock data (replace with your data later) ----
        private sealed record CourseItem(string Title, string Slug, int Students, string Author, DateTime StartDate);
        private sealed record ModuleItem(string Title, int Items);
        private sealed record UpcomingItem(string Title, string Course, string CourseSlug, DateTime Date);
        private sealed record SubmissionItem(int Id, string Assignment, string Student, string Status);

        private readonly List<CourseItem> Courses = new()
    {
        new("Introduktion till programmering","intro-prog",25,"Anna", DateTime.Today.AddDays(7)),
        new("Webb med Blazor","blazor-webb",18,"Anna", DateTime.Today.AddDays(12))
    };

        private readonly List<ModuleItem> Modules = new()
    {
        new("Grunder i C#", 6),
        new("Komponenter och routing", 4),
        new("Databindning & formulär", 5)
    };

        private readonly List<UpcomingItem> Upcoming = new()
    {
        new("Inlämning: Uppgift 1","Introduktion till programmering","intro-prog", DateTime.Today.AddDays(3)),
        new("Gruppdiskussion","Webb med Blazor","blazor-webb", DateTime.Today.AddDays(5))
    };

        private readonly List<SubmissionItem> Submissions = new()
    {
        new(101,"Uppgift 1","Erik Svensson","Ej bedömd"),
        new(102,"Uppgift 2","Sara Nilsson","Granskning"),
        new(103,"Uppgift 3","Mikael Holm","Godkänd")
    };
    }
}