using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IProjDocumentService
    {
        Task<ProjDocumentDto?> GetAsync(int id, bool trackChanges = false);
        Task<IEnumerable<ProjDocumentDto>> GetByCourseAsync(int courseId, bool trackChanges = false);
        Task<IEnumerable<ProjDocumentDto>> GetByModuleAsync(int moduleId, bool trackChanges = false);
        Task<IEnumerable<ProjDocumentDto>> GetByActivityAsync(int activityId, bool trackChanges = false);
        Task<IEnumerable<ProjDocumentDto>> GetByStudentAsync(string studentId, bool trackChanges = false);

        Task<ProjDocumentDto> UploadAsync(
            UploadProjDocumentDto meta,
            Stream content,
            string originalFileName,
            string uploaderUserId,
            CancellationToken ct = default);

        Task<(Stream stream, string downloadFileName, string contentType)?> OpenReadAsync(int id, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        Task<bool> SetStatusAsync(int documentId, DocumentStatus status, string changedByUserId, CancellationToken ct = default);

    }
}
