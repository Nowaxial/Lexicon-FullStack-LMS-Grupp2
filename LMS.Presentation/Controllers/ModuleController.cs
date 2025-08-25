using LMS.Shared.DTOs.EntitiesDtos.ModuleDto.ModuleDto;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/course/{courseId}/Modules")]
    public class ModuleController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ModuleController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }


        // GET: api/Modules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuleDto>>> GetModules(int courseId)
        {
            var modules = await _serviceManager.ModuleService.GetAllModulesAsync(courseId);
            return Ok(modules); 
        }

        // GET: api/Modules/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ModuleDto>> GetModule(int courseId, int id)
        {
            var module = await _serviceManager.ModuleService.GetModuleByIdAsync(id);
            if (module == null || module.CourseId != courseId)
                return NotFound();

            return Ok(module);
        }

        // POST: api/Modules
        [HttpPost]
        public async Task<ActionResult<ModuleDto>> CreateModule(int courseId, ModuleCreateDto dto)
        {
            var module = await _serviceManager.ModuleService.CreateModuleAsync(courseId, dto);
            return CreatedAtAction(nameof(GetModule), new 
            { 
                courseId = courseId, 
                id = module.Id 
            }, module);
        }

        // PUT: api/Modules/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModule(int courseId, int id, ModuleUpdateDto dto)
        {
            var module = await _serviceManager.ModuleService.GetModuleByIdAsync(id);
            if (module == null || module.CourseId != courseId)
                return NotFound();

            var result = await _serviceManager.ModuleService.UpdateModuleAsync(id, dto);
            if (!result)
                return StatusCode(500, "A problem happened while handling your request.");
            return NoContent();
        }

        // DELETE: api/Modules/5
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
