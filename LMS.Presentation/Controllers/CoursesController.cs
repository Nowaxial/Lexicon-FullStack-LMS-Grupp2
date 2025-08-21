using AutoMapper;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IMapper _mapper;

        public CoursesController(ICourseService courseService, IMapper mapper)
        {
            _courseService = courseService;
            _mapper = mapper;
        }

        // GET: api/courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var courses = await _courseService.GetAllCoursesAsync();
            return Ok(_mapper.Map<IEnumerable<CourseDto>>(courses));
        }

        // GET: api/courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDto>> GetCourse(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            return Ok(_mapper.Map<CourseDto>(course));
        }

        // POST: api/courses
        [HttpPost]
        public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto dto)
        {
            var course = _mapper.Map<Course>(dto);

            await _courseService.CreateCourseAsync(course);

            var result = _mapper.Map<CourseDto>(course);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, result);
        }

        // PUT: api/courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, UpdateCourseDto dto)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            _mapper.Map(dto, course);
            await _courseService.UpdateCourseAsync(course);

            return NoContent();
        }

        // DELETE: api/courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            await _courseService.DeleteCourseAsync(course);
            return NoContent();
        }
    }
}