using LMS.Shared.DTOs.Common;
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
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        if (page < 1 || size < 1 || size > 200)
            return BadRequest("Invalid paging.");

        var result = await _services.UserService.GetUsersAsync(search, page, size);
        return Ok(result);
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        var user = await _services.UserService.GetUserByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    // PUT: api/users/{id}  <-- Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _services.UserService.UpdateUserAsync(id, dto);
        return updated ? NoContent() : NotFound();
    }

    // PATCH: api/users/{id}  <-- Optional partial update
    //[HttpPatch("{id}")]
    //public async Task<IActionResult> PatchUser(string id, [FromBody] UpdateUserDto dto)
    //{
    //    if (!ModelState.IsValid)
    //        return BadRequest(ModelState);

    //    var updated = await _services.UserService.UpdateUserPartialAsync(id, dto);
    //    return updated ? NoContent() : NotFound();
    //}

    // PUT: api/users/{id}/roles
    [HttpPut("{id}/roles")]
    public async Task<IActionResult> SetUserRoles(string id, [FromBody] SetRolesDto dto)
    {
        var ok = await _services.UserService.SetUserRolesAsync(id, dto.Roles);
        return ok ? NoContent() : NotFound();
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _services.UserService.CreateUserAsync(dto);
        if (created is null)
        {
            return BadRequest("Failed to create user. Check password policy or duplicate email/username.");
        }

        return CreatedAtAction(nameof(GetUserById), new { id = created.Id }, created);
    }
}