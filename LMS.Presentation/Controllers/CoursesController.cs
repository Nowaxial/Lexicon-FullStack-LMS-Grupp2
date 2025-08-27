using AutoMapper;
using Domain.Models.Entities;
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
        private readonly IMapper _mapper;

        public CoursesController(IServiceManager services, IMapper mapper)
        {
            _services = services;
            _mapper = mapper;
        }

        // GET: api/courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses(bool includeModules)
        {
            var courses = await _services.CourseService.GetAllCoursesAsync(includeModules);
            return Ok(_mapper.Map<IEnumerable<CourseDto>>(courses));
        }

        // GET: api/courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDto>> GetCourse(int id, bool includeModules)
        {
            var course = await _services.CourseService.GetCourseByIdAsync(id, includeModules);
            if (course == null)
                return NotFound();

            return Ok(_mapper.Map<CourseDto>(course));
        }

        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var courses = await _services.CourseService.GetCoursesByUserAsync(userId);
            return Ok(courses);
        }

        // POST: api/courses
        [HttpPost]
        public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto dto)
        {
            var course = _mapper.Map<Course>(dto);

            await _services.CourseService.CreateCourseAsync(course);

            var result = _mapper.Map<CourseDto>(course);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, result);
        }

        // PUT: api/courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, UpdateCourseDto dto)
        {
            var course = await _services.CourseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            _mapper.Map(dto, course);
            await _services.CourseService.UpdateCourseAsync(course);

            return NoContent();
        }

        // DELETE: api/courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _services.CourseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            await _services.CourseService.DeleteCourseAsync(course);
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