/// <summary>
/// Metadata for an upload. The file bytes are not in this DTO; they are sent
/// as a separate IFormFile in a multipart/form-data request.
/// </summary>

namespace LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos
{
    public record UploadProjDocumentDto
    {
        public string DisplayName { get; init; } = null!;
        public string? Description { get; init; }

        public int? CourseId { get; init; }
        public int? ModuleId { get; init; }
        public int? ActivityId { get; init; }
        public string? StudentId { get; init; }

        public bool IsSubmission { get; init; }
    }
}
