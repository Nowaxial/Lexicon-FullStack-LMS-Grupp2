using LMS.Shared.DTOs.UsersDtos;
using LMS.Shared.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IUserService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(string? search, int page, int size);
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<bool> UpdateUserAsync(string userId, UpdateUserDto dto);
        Task<bool> SetUserRolesAsync(string userId, IEnumerable<string> roles);
    }
}
