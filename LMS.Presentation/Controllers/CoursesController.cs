using LMS.Shared.DTOs.EntitiesDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System.Security.Claims;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly IServiceManager _services;

        public CoursesController(IServiceManager services)
        {
            _services = services;
        }

        // GET: api/courses
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses(bool includeModules, bool includeActivities, bool trackChanges = false)
        {
            var coursesDtos = await _services.CourseService.GetAllCoursesAsync(includeModules, includeActivities, trackChanges);
            return Ok(coursesDtos);
        }

        // GET: api/courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDto>> GetCourse(int id, bool includeModules, bool includeActivities, bool trackChanges = false)
        {
            var courseDto = await _services.CourseService.GetCourseByIdAsync(id, includeModules, includeActivities, trackChanges);
            if (courseDto == null) return NotFound();
            return Ok(courseDto);
        }

        // GET: api/courses/my
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyCourses(bool includeModules, bool trackChanges = false)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var coursesDtos = await _services.CourseService.GetCoursesByUserAsync(userId, includeModules, trackChanges);
            return Ok(coursesDtos);
        }

        // POST: api/courses
        [HttpPost]
        public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto dto)
        {
            var created = await _services.CourseService.CreateCourseAsync(dto);
            return CreatedAtAction(nameof(GetCourse), new { id = created.Id }, created);
        }

        // PUT: api/courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, UpdateCourseDto dto)
        {
            // check existence using DTO service
            var existing = await _services.CourseService.GetCourseByIdAsync(id);
            if (existing == null) return NotFound();

            var ok = await _services.CourseService.UpdateCourseAsync(id, dto);
            if (!ok) return StatusCode(500, "A problem happened while handling your request.");
            return NoContent();
        }

        // DELETE: api/courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var existing = await _services.CourseService.GetCourseByIdAsync(id);
            if (existing == null) return NotFound();

            var ok = await _services.CourseService.DeleteCourseAsync(id);
            if (!ok) return StatusCode(500, "A problem happened while handling your request.");
            return NoContent();
        }

        // POST: api/courses/{courseId}/users/{userId}
        [HttpPost("{courseId}/users/{userId}")]
        public async Task<IActionResult> AssignUserToCourse(int courseId, string userId)
        {
            var ok = await _services.CourseService.AssignUserAsync(courseId, userId);
            return ok ? NoContent() : NotFound();
        }

        // DELETE: api/courses/{courseId}/users/{userId}
        [HttpDelete("{courseId}/users/{userId}")]
        public async Task<IActionResult> UnassignUserFromCourse(int courseId, string userId)
        {
            var ok = await _services.CourseService.UnassignUserAsync(courseId, userId);
            return ok ? NotFound() : NoContent();
        }

        // GET: api/courses/{courseId}/users
        [HttpGet("{courseId}/users")]
        public async Task<IActionResult> GetUsersForCourse(int courseId)
        {
            var users = await _services.CourseService.GetUsersForCourseAsync(courseId);
            return Ok(users);
        }
    }
}
