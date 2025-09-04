using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Infractructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infractructure.Repositories
{
    public sealed class ProjDocumentRepository
        : RepositoryBase<ProjDocument>, IProjDocumentRepository
    {
        public ProjDocumentRepository(ApplicationDbContext context) : base(context) { }

        public Task<ProjDocument?> GetByIdAsync(int id, bool trackChanges = false) =>
            FindByCondition(d => d.Id == id, trackChanges)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<ProjDocument>> GetByCourseAsync(int courseId, bool trackChanges = false) =>
            await FindByCondition(d => d.CourseId == courseId, trackChanges)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<IEnumerable<ProjDocument>> GetByModuleAsync(int moduleId, bool trackChanges = false) =>
            await FindByCondition(d => d.ModuleId == moduleId, trackChanges)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<IEnumerable<ProjDocument>> GetByActivityAsync(int activityId, bool trackChanges = false) =>
            await FindByCondition(d => d.ActivityId == activityId, trackChanges)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<IEnumerable<ProjDocument>> GetByStudentAsync(string studentId, bool trackChanges = false) =>
            await FindByCondition(d => d.StudentId == studentId, trackChanges)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
    }
}
