using System.Security.Claims;
using LMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // Teacher endpoints
    [HttpGet]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
        return Ok(notifications);
    }

    [HttpPost("{id}/read")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok();
    }

    [HttpPost("{id}/unread")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> MarkAsUnread(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.MarkAsUnreadAsync(id, userId);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteNotification(string id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Ok();
    }

    // Contact message endpoints
    [HttpGet("contact-messages")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetContactMessages()
    {
        var messages = await _notificationService.GetAllContactMessagesAsync();
        return Ok(messages);
    }

    [HttpGet("contact-messages/{id}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetContactMessage(Guid id)
    {
        var message = await _notificationService.DecryptMessageAsync(id);
        if (message == null)
            return NotFound();

        return Ok(message);
    }

    // Public endpoint for sending contact messages
    [HttpPost("contact")]
    [AllowAnonymous]
    public async Task<IActionResult> SendContactMessage([FromBody] ContactMessageDto message)
    {
        try
        {
            await _notificationService.SaveContactMessageAsync(message);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
