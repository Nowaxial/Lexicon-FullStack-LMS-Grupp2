namespace LMS.Blazor.Components.Pages
{
    public partial class Home
    {
        private sealed record Course(
            string Title, string Summary, string Level, string Duration, DateTime StartDate, string Link);

        // Demo data – replace with your real API later
        private readonly List<Course> Courses = new()
    {
        new("C# Grund", "Lär dig grunderna i C#, syntax och objektorientering.", "Nybörjare", "6 veckor", DateTime.Today.AddDays(7), "kurser/csharp-grund"),
        new("Webb med Blazor", "Bygg moderna webbappar med Blazor och .NET.", "Medel", "5 veckor", DateTime.Today.AddDays(14), "kurser/blazor-webb"),
        new("SQL & Databaser", "Relationsdatabaser, SQL-frågor och indexering.", "Medel", "4 veckor", DateTime.Today.AddDays(10), "kurser/sql-db")
    };
    }
}