using LMS.Shared.DTOs.AuthDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace LMS.Presentation.Controllers;

[Route("api/token")]
[ApiController]
public class TokenController(IAuthService authenticationService) : ControllerBase
{
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Refresh JWT token",
        Description = "Takes an existing access token and refresh token, validates them, and issues a new token pair."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Token successfully refreshed", typeof(TokenDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid token data or refresh token expired")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token could not be refreshed due to authentication failure")]
    public async Task<ActionResult<TokenDto>> RefreshToken(TokenDto token) =>
         Ok(await authenticationService.RefreshTokenAsync(token));
}
