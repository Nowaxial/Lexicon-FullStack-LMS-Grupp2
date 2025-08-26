using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos
{
    public record ModuleCreateDto
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public DateOnly Starts { get; init; }
        public DateOnly Ends { get; init; }

        public int CourseId { get; init; }
    }
}