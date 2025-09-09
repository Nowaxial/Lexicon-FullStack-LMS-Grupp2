using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [NotMapped]
    public string? FullName => $"{FirstName} {LastName}".Trim();

    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpireTime { get; set; }

    // 🔹 Add navigation property
    public ICollection<CourseUser> CourseUsers { get; set; } = new List<CourseUser>();
}

