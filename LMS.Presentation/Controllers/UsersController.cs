using LMS.Shared.DTOs.UsersDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = "Teacher,Admin")]
public class UsersController : ControllerBase
{
    private readonly IServiceManager _services;

    public UsersController(IServiceManager services)
    {
        _services = services;
    }

    // GET: api/users?search=...&page=1&size=20
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1 || size < 1 || size > 200) return BadRequest("Invalid paging.");
        var result = await _services.UserService.GetUsersAsync(search, page, size);
        return Ok(result); // return a paged DTO { items, total, page, size }
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await _services.UserService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    // OPTIONAL: update limited profile fields
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        var updated = await _services.UserService.UpdateUserAsync(id, dto);
        return updated ? NoContent() : NotFound();
    }

    // OPTIONAL: role management (admin only)
    [HttpPut("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetUserRoles(string id, [FromBody] SetRolesDto dto)
    {
        var ok = await _services.UserService.SetUserRolesAsync(id, dto.Roles);
        return ok ? NoContent() : NotFound();
    }
}