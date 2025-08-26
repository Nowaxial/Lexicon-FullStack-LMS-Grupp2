using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

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
        public async Task<ActionResult<ProjActivityDto>> CreateActivity(int moduleId, CreateProjActivityDto dto)
        {
            var createdActivity = await _serviceManager.ProjActivityService.CreateActivityAsync(moduleId, dto);
            return CreatedAtAction(nameof(GetActivity), new { moduleId, id = createdActivity.Id }, createdActivity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActivity(int moduleId, int id, UpdateProjActivityDto dto)
        {
            var activity = await _serviceManager.ProjActivityService.GetActivityByIdAsync(id);
            if (activity == null || activity.ModuleId != moduleId)
                return NotFound();

            await _serviceManager.ProjActivityService.UpdateActivityAsync(id, dto);
            return NoContent();
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