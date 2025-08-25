using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Infractructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infractructure.Repositories
{
    public class ProjActivityRepository(ApplicationDbContext context) : RepositoryBase<ProjActivity>(context), IProjActivityRepository
    {
        public async Task<IEnumerable<ProjActivity>> GetActivitiesByModuleIdAsync(int moduleId, bool trackChanges = false)
        {
            return await FindByCondition(a => a.ModuleId == moduleId, trackChanges).ToListAsync();
        }
    }
}
