using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [NotMapped]
    public string? FullName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpireTime { get; set; }
}
