using System.ComponentModel.DataAnnotations;

namespace LMS.Shared.DTOs.EntitiesDtos.ModulesDtos
{
    public record ModuleUpdateDto
    {
        [Required]
        public int Id { get; init; }

        [Required(ErrorMessage = "Module name is required")]
        [StringLength(100, ErrorMessage = "Module name must be at most 100 characters")]
        public string Name { get; init; } = null!;

        [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
        public string? Description { get; init; }

        [Required]
        public DateOnly Starts { get; init; }

        [Required]
        public DateOnly Ends { get; init; }

        [Required]
        public int CourseId { get; init; }
    }
}