namespace LMS.Blazor.Client.Models;

// Add properties to this class and update the server and client AuthenticationStateProviders
// to expose more information about the authenticated user.
public class UserInfo
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public List<string> Roles { get; set; } = [];
}