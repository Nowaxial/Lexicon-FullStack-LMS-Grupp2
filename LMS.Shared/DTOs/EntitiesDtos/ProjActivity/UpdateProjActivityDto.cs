using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos.ProjActivity
{
    public class UpdateProjActivityDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Type { get; set; } = null!;
        public DateTime Starts { get; set; }
        public DateTime Ends { get; set; }
    }
}
