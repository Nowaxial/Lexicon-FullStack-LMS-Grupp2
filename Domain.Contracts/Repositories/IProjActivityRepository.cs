using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface IProjActivityRepository : IRepositoryBase<ProjActivity>, IInternalRepositoryBase<ProjActivity>
    {
        Task<IEnumerable<ProjActivity>> GetActivitiesByModuleIdAsync(int moduleId, bool trackChanges = false);
    }
}
