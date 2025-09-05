using Domain.Models.Entities;
using LMS.Infractructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DocumentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Upload
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? description, [FromForm] string uploadedBy)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var document = new Domain.Models.Entities.ProjDocument
        {
            FileName = file.FileName,
            Description = description,
            UploadedByUserId = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return Ok(new { document.Id, document.FileName });
    }

    // Download
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null)
            return NotFound();

        return File(doc, "application/octet-stream", doc.DisplayName);
    }

    private IActionResult File(ProjDocument doc, string v, string displayName)
    {
        throw new NotImplementedException();
    }

    // List
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjDocument>>> GetDocuments()
    {
        var docs = await _context.ProjDocuments.ToListAsync();
        return Ok(docs);
    }
}