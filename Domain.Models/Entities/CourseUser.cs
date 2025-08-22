using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities
{
    public class CourseUser
    {
        public string UserId { get; set; } = null!; // FK to AspNetUser
        public int CourseId { get; set; }
        public bool IsTeacher { get; set; }

        // Navigation
        public Course Course { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}
