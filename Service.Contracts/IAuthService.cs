using LMS.Shared.DTOs.AuthDtos;
using Microsoft.AspNetCore.Identity;

namespace Service.Contracts;
public interface IAuthService
{
    Task<TokenDto> CreateTokenAsync(bool addTime);
    Task<TokenDto> RefreshTokenAsync(TokenDto token);
    Task<IdentityResult> RegisterUserAsync(UserRegistrationDto userRegistrationDto);
    Task<bool> ValidateUserAsync(UserAuthDto user);
}
