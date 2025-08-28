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

        public async Task<IEnumerable<Module>> GetAllAsync(int courseId, bool includeActivities = false,  bool trackChanges = false)
        {

            var query = FindAll(trackChanges);

                if (includeActivities)
                {
                query = query.Include(m => m.Activities);
                                 //.Include(m => m.Documents);
                }

            return await query.ToListAsync();
        }

        public async Task<Module?> GetByIdAsync(int id, bool includeActivities = false, bool trackChanges = false)
        {
            var query = FindByCondition(m => m.Id.Equals(id), trackChanges);

            if (includeActivities)
            {
                query = query.Include(m => m.Activities);
                             //.Include(m => m.Documents);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Module>> GetByCourseAsync(int courseId, bool includeActivities = false, bool trackChanges = false)
        {
            var query = FindByCondition(m => m.CourseId.Equals(courseId), trackChanges);

            if (includeActivities) {
                query = query.Include(m => m.Activities);
                //.Include(m => m.Documents);
            }
            return await query.ToListAsync();
        }
    }
}