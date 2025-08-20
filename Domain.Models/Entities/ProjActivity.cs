using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities
{
    public class ProjActivity
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime Starts { get; set; }
        public DateTime Ends { get; set; }

        // Foreign Key
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;

        // Navigation
        public ICollection<ProjDocument> Documents { get; set; } = new List<ProjDocument>(); // ✅ added
    }
}
