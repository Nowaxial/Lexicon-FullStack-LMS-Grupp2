using System.ComponentModel.DataAnnotations;

namespace LMS.Shared.DTOs.UsersDtos
{
    public record UpdateUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username must be at most 50 characters")]
        public string? UserName { get; init; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string? Email { get; init; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name must be at most 50 characters")]
        public string? FirstName { get; init; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name must be at most 50 characters")]
        public string? LastName { get; init; }

        [Required(ErrorMessage = "At least one role is required")]
        public List<string>? Roles { get; init; }
    }
}