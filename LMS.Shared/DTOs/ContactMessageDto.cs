using System.ComponentModel.DataAnnotations;

namespace LMS.Shared.DTOs;

public class ContactMessageDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Message { get; set; } = string.Empty;
}

public class NotificationItem
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public List<string> ReadBy { get; set; } = new();
    public List<string> TargetTeachers { get; set; } = new();
}

public class ContactMessageItem
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Subject { get; set; } = string.Empty;
}
