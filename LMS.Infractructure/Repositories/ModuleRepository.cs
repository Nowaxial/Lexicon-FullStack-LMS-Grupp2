using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Infractructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infractructure.Repositories
{
    public class ModuleRepository : RepositoryBase<Module>, IModuleRepository
    {
        public ModuleRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Module>> GetAllAsync(bool trackChanges = false)
        {
            return await FindAll(trackChanges)
                .Include(m => m.Course)
                .OrderBy(m => m.Starts)
                .ToListAsync();
        }

        public async Task<Module?> GetByIdAsync(int id, bool trackChanges = false)
        {
            return await FindByCondition(m => m.Id.Equals(id), trackChanges)
                .Include(m => m.Course)
                .Include(m => m.Activities)
                .Include(m => m.Documents)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Module>> GetByCourseAsync(int courseId, bool trackChanges = false)
        {
            return await FindByCondition(m => m.CourseId.Equals(courseId), trackChanges)
                .Include(m => m.Course)
                .Include(m => m.Activities)
                .Include(m => m.Documents)
                .OrderBy(m => m.Starts)
                .ToListAsync();
        }
    }
}