using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.Common
{
    public class SearchResultDto
    {
        public List<CourseDto> Courses { get; set; } = new();
        public List<ModuleDto> Modules { get; set; } = new();
        public List<ProjActivityDto> Activities { get; set; } = new();
    }
}
