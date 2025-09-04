using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public sealed class ProjDocumentsController : ControllerBase
    {
        private readonly IProjDocumentService _service;

        public ProjDocumentsController(IProjDocumentService service)
        {
            _service = service;
        }

        // -------- Upload (multipart/form-data) --------
        // Form fields: DisplayName, Description, CourseId, ModuleId, ActivityId, StudentId, IsSubmission, File
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024 * 200)] // 200 MB example
        [RequestSizeLimit(1024L * 1024 * 200)]
        public async Task<ActionResult<ProjDocumentDto>> Upload([FromForm] UploadForm form, CancellationToken ct)
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
                CourseId = form.CourseId,
                ModuleId = form.ModuleId,
                ActivityId = form.ActivityId,
                StudentId = form.StudentId,
                IsSubmission = form.IsSubmission
            };

            await using var stream = form.File.OpenReadStream();
            var dto = await _service.UploadAsync(meta, stream, form.File.FileName, userId, ct);

            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        // -------- Get metadata by id --------
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjDocumentDto>> Get(int id, CancellationToken ct)
        {
            var doc = await _service.GetAsync(id, trackChanges: false);
            return doc is null ? NotFound() : Ok(doc);
        }

        // -------- Download file --------
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
        public sealed class UploadForm
        {
            public string? DisplayName { get; set; }
            public string? Description { get; set; }

            public int? CourseId { get; set; }
            public int? ModuleId { get; set; }
            public int? ActivityId { get; set; }
            public string? StudentId { get; set; }

            public bool IsSubmission { get; set; }

            public IFormFile? File { get; set; }
        }
    }
}
