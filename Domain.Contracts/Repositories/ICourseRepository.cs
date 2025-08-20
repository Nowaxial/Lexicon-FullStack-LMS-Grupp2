using Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts.Repositories
{
    public interface ICourseRepository : IRepositoryBase<Course>, IInternalRepositoryBase<Course>
    {
        Task<Course?> GetCourseWithModulesAsync(int id, bool trackChanges = false);
    }
}
