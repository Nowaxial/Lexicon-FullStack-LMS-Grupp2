using System.Diagnostics;

namespace Domain.Models.Entities
{
    public class ProjDocument
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }

        // Foreign Keys
        public string UploadedByUserId { get; set; } = null!; // AspNetUser FK
        public int? CourseId { get; set; }
        public Course? Course { get; set; }
        public int? ModuleId { get; set; }
        public Module? Module { get; set; }
        public int? ActivityId { get; set; }
        public ProjActivity? Activity { get; set; }
        public string? StudentId { get; set; } // AspNetUser FK

        public bool IsSubmission { get; set; }
    }
}