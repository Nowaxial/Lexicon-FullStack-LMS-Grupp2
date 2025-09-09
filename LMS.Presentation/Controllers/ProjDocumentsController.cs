using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize]
    [Tags("Documents")]
    public sealed class ProjDocumentsController : ControllerBase
    {
        private readonly IProjDocumentService _service;
        private readonly ILogger<ProjDocumentsController> _logger;
        public ProjDocumentsController(IProjDocumentService service, ILogger<ProjDocumentsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("/api/courses/{courseId:int}/modules/{moduleId:int}/activities/{activityId:int}/documents")]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024 * 200)] // 200 MB example
        [RequestSizeLimit(1024L * 1024 * 200)]
        [ProducesResponseType(typeof(ProjDocumentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ProjDocumentDto>> Upload(
            [FromRoute] int courseId,
            [FromRoute] int moduleId,
            [FromRoute] int activityId,
            //[FromForm] string? DisplayName,
            //[FromForm] string? Description,
            //[FromForm] bool IsSubmission,
            [FromForm] UploadActivityForm form,
            CancellationToken ct)
        {
            try
            {
                if (form.File is null || form.File.Length == 0)
                    return BadRequest("File is required.");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                var meta = new UploadProjDocumentDto
                {
                    DisplayName = string.IsNullOrWhiteSpace(form.DisplayName) ? form.File.FileName : form.DisplayName,
                    Description = form.Description,
                    CourseId = courseId,
                    ModuleId = moduleId,
                    ActivityId = activityId,
                    StudentId = userId,
                    IsSubmission = form.IsSubmission
                };

                await using var stream = form.File.OpenReadStream();
                var dto = await _service.UploadAsync(meta, stream, form.File.FileName, userId, ct);

                return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Upload failed. courseId={CourseId}, moduleId={ModuleId}, activityId={ActivityId}, user={User}",
                    courseId, moduleId, activityId, User?.Identity?.Name ?? "unknown");

                return Problem(detail: ex.Message, statusCode: 500, title: "Upload failed");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjDocumentDto>> Get(int id, CancellationToken ct)
        {
            var doc = await _service.GetAsync(id, trackChanges: false);
            return doc is null ? NotFound() : Ok(doc);
        }

        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int id, CancellationToken ct)
        {
            var tuple = await _service.OpenReadAsync(id, ct);
            if (tuple is null) return NotFound();

            var (stream, downloadFileName, contentType) = tuple.Value;
            return File(stream, contentType, downloadFileName);
        }

        // -------- Delete --------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        // -------- Lists --------
        [HttpGet("by-course/{courseId:int}")]
        public async Task<ActionResult<IEnumerable<ProjDocumentDto>>> ByCourse(int courseId, CancellationToken ct)
            => Ok(await _service.GetByCourseAsync(courseId, trackChanges: false));

        [HttpGet("by-module/{moduleId:int}")]
        public async Task<ActionResult<IEnumerable<ProjDocumentDto>>> ByModule(int moduleId, CancellationToken ct)
            => Ok(await _service.GetByModuleAsync(moduleId, trackChanges: false));

        [HttpGet("by-activity/{activityId:int}")]
        public async Task<ActionResult<IEnumerable<ProjDocumentDto>>> ByActivity(int activityId, CancellationToken ct)
            => Ok(await _service.GetByActivityAsync(activityId, trackChanges: false));

        [HttpGet("by-student/{studentId}")]
        public async Task<ActionResult<IEnumerable<ProjDocumentDto>>> ByStudent(string studentId, CancellationToken ct)
            => Ok(await _service.GetByStudentAsync(studentId, trackChanges: false));

        // ----- Upload form model -----
        public sealed class UploadActivityForm
        {
            public string? DisplayName { get; set; }
            public string? Description { get; set; }

            //public int? courseid { get; set; }
            //public int? moduleid { get; set; }
            //public int? activityid { get; set; }
            //public string? studentid { get; set; }

            public bool IsSubmission { get; set; } = false;

            [FromForm(Name = "File")] 
            public IFormFile? File { get; set; }
        }



        //Set status on a document (Teacher only)
        [HttpPost("{id:int}/status")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetDocumentStatusDto dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest("Payload is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var ok = await _service.SetStatusAsync(id, dto.Status, userId, ct);
            return ok ? NoContent() : NotFound();
        }

    }
}
