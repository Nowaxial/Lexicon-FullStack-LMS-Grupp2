using System.ComponentModel.DataAnnotations;

namespace LMS.Shared.DTOs.UsersDtos
{
    public class SetRolesDto
    {
        [Required(ErrorMessage = "Roles are required")]
        [MinLength(1, ErrorMessage = "At least one role must be assigned")]
        public required IEnumerable<string> Roles { get; init; }
    }
}