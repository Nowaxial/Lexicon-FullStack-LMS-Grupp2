using Domain.Models.Entities;
using LMS.Shared.DTOs.Common;
using LMS.Shared.DTOs.UsersDtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(string? search, int page, int size)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 20;

        IQueryable<ApplicationUser> query = _userManager.Users;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                (u.UserName != null && EF.Functions.Like(u.UserName, pattern)) ||
                (u.Email != null && EF.Functions.Like(u.Email, pattern)) ||
                (u.FirstName != null && EF.Functions.Like(u.FirstName, pattern)) ||
                (u.LastName != null && EF.Functions.Like(u.LastName, pattern)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email
            })
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var u = await _userManager.Users
            .Where(x => x.Id == userId)
            .Select(x => new UserDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                FullName = x.FullName,
                UserName = x.UserName,
                Email = x.Email
            })
            .FirstOrDefaultAsync();

        return u;
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        bool anyChange = false;

        if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
        {
            user.UserName = dto.UserName;
            anyChange = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            user.Email = dto.Email;
            user.EmailConfirmed = false;
            anyChange = true;
        }

        if (!anyChange) return true;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SetUserRolesAsync(string userId, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        var distinct = roles?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();

        foreach (var role in distinct)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var create = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!create.Succeeded)
                    throw new InvalidOperationException($"Could not create role '{role}'.");
            }
        }

        var current = await _userManager.GetRolesAsync(user);
        var toRemove = current.Except(distinct, StringComparer.OrdinalIgnoreCase).ToArray();
        var toAdd = distinct.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();

        if (toRemove.Length > 0 && !(await _userManager.RemoveFromRolesAsync(user, toRemove)).Succeeded) return false;
        if (toAdd.Length > 0 && !(await _userManager.AddToRolesAsync(user, toAdd)).Succeeded) return false;

        return true;
    }
}
