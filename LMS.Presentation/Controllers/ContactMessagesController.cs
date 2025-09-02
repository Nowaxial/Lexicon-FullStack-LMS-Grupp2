using LMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Teacher")]
public class ContactMessagesController : ControllerBase
{
    private readonly IServiceManager _services;

    public ContactMessagesController(IServiceManager services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<IActionResult> GetContactMessages()
    {
        var messages = await _services.NotificationService.GetAllContactMessagesAsync();
        return Ok(messages);
    }

    [HttpGet("{id}/decrypt")]
    public async Task<IActionResult> DecryptMessage(Guid id)
    {
        var decryptedData = await _services.NotificationService.DecryptMessageAsync(id);

        if (decryptedData == null)
            return NotFound();

        return Ok(decryptedData);
    }
}
