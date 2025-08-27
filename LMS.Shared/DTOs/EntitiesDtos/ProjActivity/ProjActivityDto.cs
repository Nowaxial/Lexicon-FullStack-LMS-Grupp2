using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos.ProjActivity
{
    public class ProjActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime Starts { get; set; }
        public DateTime Ends { get; set; }
        public int ModuleId { get; set; }
    }
}
