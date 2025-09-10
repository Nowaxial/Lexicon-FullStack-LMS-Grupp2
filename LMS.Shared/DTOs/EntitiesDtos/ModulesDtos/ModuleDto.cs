using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos.ModulesDtos
{
    public record ModuleDto
    {
        public int Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public DateOnly Starts { get; init; }
        public DateOnly Ends { get; init; }

        public int CourseId { get; init; }

        public int ActivitiesCount { get; init; }
        public int DocumentsCount { get; init; }

        public CourseDto? Course { get; init; }
        public List<ProjActivityDto> Activities { get; init; } = new();


    }
}