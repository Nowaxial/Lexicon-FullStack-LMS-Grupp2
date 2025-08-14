using LMS.Shared.DTOs.Demo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LMS.Presentation.DemoController;

[Route("api/demoauth")]
[ApiController]
public class DemoAuthController : ControllerBase
{
    [HttpGet]
    [Authorize]
    [SwaggerOperation(
        Summary =     "Get demo authenticated data",
        Description = "Returns a list of demo users. Requires a valid JWT token.")]
    [SwaggerResponse(StatusCodes.Status200OK, "List of demo users", typeof(IEnumerable<DemoAuthDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized - JWT token missing or invalid")]
    public IActionResult GetDemoAuth()
    {
        return Ok(new[]{new DemoAuthDto(1, "Kalle"),
                        new DemoAuthDto(2, "Anka" ),
                        new DemoAuthDto(3, "Nisse"),
                        new DemoAuthDto(4, "Pelle")}
        );
    }
}
