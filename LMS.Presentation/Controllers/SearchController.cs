using LMS.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public SearchController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // GET: api/search?query=python&includeCourses=true&includeModules=true&includeActivities=true
        [HttpGet]
        public async Task<ActionResult<SearchResultDto>> SearchAll(
            [FromQuery] string query,
            [FromQuery] bool includeCourses = true,
            [FromQuery] bool includeModules = true,
            [FromQuery] bool includeActivities = true)
        {
            var result = new SearchResultDto();

            if (includeCourses)
                result.Courses = (await _serviceManager.CourseService.SearchCoursesAsync(query)).ToList();

            if (includeModules)
                result.Modules = (await _serviceManager.ModuleService.SearchModulesAsync(query)).ToList();

            if (includeActivities)
                result.Activities = (await _serviceManager.ProjActivityService.SearchActivitiesAsync(query)).ToList();

            return Ok(result);
        }
    }
}