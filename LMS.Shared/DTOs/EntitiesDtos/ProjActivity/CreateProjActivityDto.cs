using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.Shared.DTOs.EntitiesDtos.ProjActivity
{
    public class CreateProjActivityDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title must be at most 100 characters")]
        public string Title { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [StringLength(50, ErrorMessage = "Type must be at most 50 characters")]
        public string Type { get; set; } = null!;

        [Required]
        public DateTime Starts { get; set; }

        [Required]
        public DateTime Ends { get; set; }
    }
}