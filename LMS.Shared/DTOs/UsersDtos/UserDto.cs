using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.UsersDtos
{
    public class UserDto
    {
        public required string Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? FullName { get; init; }
        public string? UserName { get; init; }
        public string? Email { get; init; }
        public List<string> Roles { get; set; } = new();
        public bool? IsTeacher { get; init; }
    }

}
