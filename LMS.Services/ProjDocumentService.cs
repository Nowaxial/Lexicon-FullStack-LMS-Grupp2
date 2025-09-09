using AutoMapper;
using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Service.Contracts.Storage;

namespace LMS.Services
{
    public sealed class ProjDocumentService : IProjDocumentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IFileStorage _storage;
        private readonly ILogger<ProjDocumentService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public ProjDocumentService(IUnitOfWork uow, IMapper mapper, IFileStorage storage, ILogger<ProjDocumentService> logger, INotificationService notificationService, IUserService userService)
        {
            _uow = uow;
            _mapper = mapper;
            _storage = storage;
            _logger = logger;
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task<ProjDocumentDto?> GetAsync(int id, bool trackChanges = false)
        {
            var doc = await _uow.ProjDocumentRepository.GetByIdAsync(id, trackChanges);
            return _mapper.Map<ProjDocumentDto?>(doc);
        }

        public async Task<IEnumerable<ProjDocumentDto>> GetByCourseAsync(int courseId, bool trackChanges = false)
        {
            var docs = await _uow.ProjDocumentRepository.GetByCourseAsync(courseId, trackChanges);
            return _mapper.Map<IEnumerable<ProjDocumentDto>>(docs);
        }

        public async Task<IEnumerable<ProjDocumentDto>> GetByModuleAsync(int moduleId, bool trackChanges = false)
        {
            var docs = await _uow.ProjDocumentRepository.GetByModuleAsync(moduleId, trackChanges);
            return _mapper.Map<IEnumerable<ProjDocumentDto>>(docs);
        }

        public async Task<IEnumerable<ProjDocumentDto>> GetByActivityAsync(int activityId, bool trackChanges = false)
        {
            var docs = await _uow.ProjDocumentRepository.GetByActivityAsync(activityId, trackChanges);
            return _mapper.Map<IEnumerable<ProjDocumentDto>>(docs);
        }

        public async Task<IEnumerable<ProjDocumentDto>> GetByStudentAsync(string studentId, bool trackChanges = false)
        {
            var docs = await _uow.ProjDocumentRepository.GetByStudentAsync(studentId, trackChanges);
            return _mapper.Map<IEnumerable<ProjDocumentDto>>(docs);
        }

        public async Task<ProjDocumentDto> UploadAsync(
            UploadProjDocumentDto meta,
            Stream content,
            string originalFileName,
            string uploaderUserId,
            CancellationToken ct = default)
        {
            // 1) Save the binary
            var relativePath = await _storage.SaveAsync(content, originalFileName, ct);

            // 2) Persist metadata
            var entity = _mapper.Map<ProjDocument>(meta);
            entity.FileName = relativePath;                   // store relative path
            entity.UploadedAt = DateTime.UtcNow;
            entity.UploadedByUserId = uploaderUserId;

            _uow.ProjDocumentRepository.Create(entity);
            await _uow.CompleteAsync();

            // 3) Send notification
            if (entity.IsSubmission && entity.ActivityId.HasValue)
            {
                var doc = await _uow.ProjDocumentRepository.GetByIdWithDetailsAsync(entity.Id, trackChanges: false);

                var user = await _userService.GetUserByIdAsync(uploaderUserId);
                var studentName = user?.FullName ?? "Okänd student";
                var courseName = doc?.Course?.Name ?? "Okänd kurs";
                var moduleName = doc?.Module?.Name ?? "Okänd modul";
                var activityTitle = doc?.Activity?.Title ?? "Okänd aktivitet";

                await _notificationService.NotifyFileUploadAsync(studentName, courseName, moduleName, activityTitle, entity.DisplayName, entity.Id);
            }

            return _mapper.Map<ProjDocumentDto>(entity);
        }

        public async Task<(Stream stream, string downloadFileName, string contentType)?> OpenReadAsync(int id, CancellationToken ct = default)
        {
            var doc = await _uow.ProjDocumentRepository.GetByIdAsync(id, trackChanges: false);
            if (doc is null) return null;

            var stream = await _storage.OpenReadAsync(doc.FileName, ct);

            // Build a nice download name (DisplayName + original extension)
            var ext = Path.GetExtension(doc.FileName);
            var safeBase = string.IsNullOrWhiteSpace(doc.DisplayName) ? "file" : doc.DisplayName;
            var downloadName = Path.GetFileNameWithoutExtension(safeBase) + ext;

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(downloadName, out var contentType))
                contentType = "application/octet-stream";

            return (stream, downloadName, contentType);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var doc = await _uow.ProjDocumentRepository.GetByIdAsync(id, trackChanges: true);
            if (doc is null) return false;

            try { await _storage.DeleteAsync(doc.FileName, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete blob for document {Id}", id); }

            // Remove related notifications
            await _notificationService.DeleteNotificationByDocumentIdAsync(id);

            _uow.ProjDocumentRepository.Delete(doc);
            await _uow.CompleteAsync();
            return true;
        }



        public async Task<bool> SetStatusAsync(int documentId, DocumentStatus status, string changedByUserId, CancellationToken ct = default)
        {
            if (documentId <= 0) throw new ArgumentOutOfRangeException(nameof(documentId));
            if (string.IsNullOrWhiteSpace(changedByUserId)) throw new ArgumentException("User is required.", nameof(changedByUserId));

            var persisted = status.ToString();

            var ok = await _uow.ProjDocumentRepository.SetStatusAsync(documentId, persisted, changedByUserId, ct);
            
            await _uow.CompleteAsync();
            return ok;
        }

    }
}
