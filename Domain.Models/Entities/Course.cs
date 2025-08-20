using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }

        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<CourseUser> CourseUsers { get; set; } = new List<CourseUser>();
        public ICollection<ProjDocument> Documents { get; set; } = new List<ProjDocument>();
    }
}
