using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos
{
    public class CreateCourseDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }
    }
}
