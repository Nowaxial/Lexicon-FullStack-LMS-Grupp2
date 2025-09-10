using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/course/{courseId:int}/Modules")]
    public class ModuleController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ModuleController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // GET: api/course/{courseId}/Modules?includeActivities=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuleDto>>> GetModules(
            int courseId,
            [FromQuery] bool includeActivities = false)
        {
            var modules = await _serviceManager.ModuleService
                .GetAllModulesAsync(courseId, includeActivities);

            return Ok(modules);
        }

        // GET: api/course/{courseId}/Modules/{id}?includeActivities=true
        [HttpGet("{id}")]
        public async Task<ActionResult<ModuleDto>> GetModule(
            int courseId,
            int id,
            [FromQuery] bool includeActivities = false)
        {
            var module = await _serviceManager.ModuleService
                .GetModuleByIdAsync(id, includeActivities);

            if (module == null || module.CourseId != courseId)
                return NotFound();

            return Ok(module);
        }

        // POST: api/course/{courseId}/Modules
        [HttpPost]
        public async Task<ActionResult<ModuleDto>> CreateModule(
            int courseId,
            [FromBody] ModuleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var module = await _serviceManager.ModuleService.CreateModuleAsync(courseId, dto);

                return CreatedAtAction(
                    nameof(GetModule),
                    new { courseId, id = module.Id },
                    module
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/course/{courseId}/Modules/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModule(
            int courseId,
            int id,
            [FromBody] ModuleUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var module = await _serviceManager.ModuleService.GetModuleByIdAsync(id);
            if (module == null || module.CourseId != courseId)
                return NotFound();

            try
            {
                var result = await _serviceManager.ModuleService.UpdateModuleAsync(id, dto);
                if (!result)
                    return StatusCode(500, "A problem happened while handling your request.");

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE: api/course/{courseId}/Modules/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(int courseId, int id)
        {
            var module = await _serviceManager.ModuleService.GetModuleByIdAsync(id);
            if (module == null || module.CourseId != courseId)
                return NotFound();

            var result = await _serviceManager.ModuleService.DeleteModuleAsync(id);
            if (!result)
                return StatusCode(500, "A problem happened while handling your request.");

            return NoContent();
        }
    }
}