using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System.ComponentModel.DataAnnotations;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/modules/{moduleId:int}/activities")]
    [Authorize]
    [Tags("Activities")]
    public class ProjActivitiesController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ProjActivitiesController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjActivityDto>>> GetActivities(int moduleId)
        {
            var activities = await _serviceManager.ProjActivityService.GetActivitiesByModuleIdAsync(moduleId);
            return Ok(activities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjActivityDto>> GetActivity(int moduleId, int id)
        {
            var activity = await _serviceManager.ProjActivityService.GetActivityByIdAsync(id);
            if (activity == null || activity.ModuleId != moduleId)
                return NotFound();
            return Ok(activity);
        }

        [HttpPost]
        public async Task<ActionResult<ProjActivityDto>> CreateActivity(int moduleId, [FromBody] CreateProjActivityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdActivity = await _serviceManager.ProjActivityService.CreateActivityAsync(moduleId, dto);
                return CreatedAtAction(nameof(GetActivity), new { moduleId, id = createdActivity.Id }, createdActivity);
            }
            catch (ValidationException ex)
            {
                // Catches business rule violations (dates, module span, etc.)
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActivity(int moduleId, int id, [FromBody] UpdateProjActivityDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await  _serviceManager.ProjActivityService.UpdateActivityAsync(id, dto);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(int moduleId, int id)
        {
            var activity = await _serviceManager.ProjActivityService.GetActivityByIdAsync(id);
            if (activity == null || activity.ModuleId != moduleId)
                return NotFound();

            await _serviceManager.ProjActivityService.DeleteActivityAsync(id);
            return NoContent();
        }
    }
}