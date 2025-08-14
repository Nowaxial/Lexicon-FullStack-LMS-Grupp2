using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Configurations;
public class JwtSettings
{
    public const string Section = "JwtSettings";

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Required]
    public int Expires { get; set; }

    [Required]
    public string SecretKey { get; set; } = null!;
}
