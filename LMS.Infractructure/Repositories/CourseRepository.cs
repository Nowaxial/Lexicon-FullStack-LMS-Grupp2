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
    public class CourseRepository : RepositoryBase<Course>, ICourseRepository
    {
        public CourseRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Course?> GetCourseWithModulesAsync(int id, bool trackChanges = false)
        {
            return await FindByCondition(c => c.Id == id, trackChanges)
                            .Include(c => c.Modules)
                            .FirstOrDefaultAsync();
        }
    }
}
