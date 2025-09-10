namespace LMS.Shared.DTOs.EntitiesDtos.ProjActivity
{
    public class ProjActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime Starts { get; set; }
        public DateTime Ends { get; set; }

        // Foreign keys
        public int ModuleId { get; set; }
        public int CourseId { get; set; }

        // Enriched fields for display (optional, filled by service layer)
        public string? ModuleName { get; set; }
        public string? CourseName { get; set; }

    }
}