using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;

namespace Service.Contracts
{
    public interface IProjActivityService
    {
        Task<IEnumerable<ProjActivityDto>> GetAllActivitiesAsync(bool trackChanges = false);
        Task<ProjActivityDto?> GetActivityByIdAsync(int id, bool trackChanges = false);
        Task<IEnumerable<ProjActivityDto>> GetActivitiesByModuleIdAsync(int moduleId, bool trackChanges = false);
        Task<ProjActivityDto> CreateActivityAsync(int moduleId, CreateProjActivityDto dto);
        Task UpdateActivityAsync(int id, UpdateProjActivityDto dto);
        Task DeleteActivityAsync(int id);

        Task<IEnumerable<ProjActivityDto>> SearchActivitiesAsync(string query, bool trackChanges = false);
    }
}
