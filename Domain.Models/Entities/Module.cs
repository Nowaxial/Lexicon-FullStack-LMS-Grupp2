using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities
{
    public class Module
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Modul name is a required field.")]
        [MaxLength(60, ErrorMessage = "Maximum length for the Name is 60 characters.")]
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateOnly Starts { get; set; }
        public DateOnly Ends { get; set; }

        // Foreign Key
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        // Navigation
        public ICollection<ProjActivity> Activities { get; set; } = new List<ProjActivity>(); // ✅ added
        public ICollection<ProjDocument> Documents { get; set; } = new List<ProjDocument>();  // ✅ added
    }
}