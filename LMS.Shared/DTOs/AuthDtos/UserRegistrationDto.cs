using System.ComponentModel.DataAnnotations;

namespace LMS.Shared.DTOs.AuthDtos;
public record UserRegistrationDto
{
    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string UserName { get; init; } = string.Empty;

    //Optional if you want to add user to role when you register user
    //UI have to be updated to support this
    public string? Role { get; init; } = string.Empty;
}