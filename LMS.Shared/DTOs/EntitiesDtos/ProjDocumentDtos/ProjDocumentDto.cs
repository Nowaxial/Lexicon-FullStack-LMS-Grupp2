using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos
{
    public record ProjDocumentDto
    {
        public int Id { get; init; }
        public string DisplayName { get; init; } = null!;
        public string FileName { get; init; } = null!;
        public string? Description { get; init; }
        public DateTime UploadedAt { get; init; }
        public string UploadedByUserId { get; init; } = null!;
        public string? UploadedByName { get; init; }  // optional convenience from API

        public int? CourseId { get; init; }
        public int? ModuleId { get; init; }
        public int? ActivityId { get; init; }
        public string? StudentId { get; init; }
        public bool IsSubmission { get; init; }

        public string? ContentType { get; init; }  
        public long? Size { get; init; }   
    }
}
