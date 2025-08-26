using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/activities")]
    public class ProjActivitiesController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ProjActivitiesController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjActivityDto>>> GetActivities()
        {
            var activities = await _serviceManager.ProjActivityService.GetAllActivitiesAsync();
            return Ok(activities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjActivityDto>> GetActivity(int id)
        {
            var activity = await _serviceManager.ProjActivityService.GetActivityByIdAsync(id);
            if (activity == null) return NotFound();
            return Ok(activity);
        }


        [HttpGet("module/{moduleId}")]
        public async Task<ActionResult<IEnumerable<ProjActivityDto>>> GetActivitiesByModule(int moduleId)
        {
            var activities = await _serviceManager.ProjActivityService.GetActivitiesByModuleIdAsync(moduleId);
            return Ok(activities);
        }

        [HttpPost]
        public async Task<ActionResult<ProjActivityDto>> CreateActivity(CreateProjActivityDto dto)
        {
            var createdActivity = await _serviceManager.ProjActivityService.CreateActivityAsync(dto);
            return CreatedAtAction(nameof(GetActivity), new { id = createdActivity.Id }, createdActivity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActivity(int id, UpdateProjActivityDto dto)
        {
            await _serviceManager.ProjActivityService.UpdateActivityAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(int id)
        {
            await _serviceManager.ProjActivityService.DeleteActivityAsync(id);
            return NoContent();
        }
    }
}
