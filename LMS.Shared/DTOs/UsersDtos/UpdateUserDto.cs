using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.UsersDtos
{
    public class UpdateUserDto
    {
        public string? FullName { get; init; }
        public string? Email { get; init; }
        public string? UserName { get; init; }
    }
}
