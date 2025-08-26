using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos.ModuleDto.ModuleDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IModuleService
    {

        Task<IEnumerable<ModuleDto>> GetAllModulesAsync(int courseId, bool trackChanges = false);
        Task<ModuleDto?> GetModuleByIdAsync(int id, bool trackChanges = false);

        Task<ModuleDto> CreateModuleAsync(int courseId, ModuleCreateDto dto);
        Task<bool> UpdateModuleAsync(int id, ModuleUpdateDto dto);
        Task<bool> DeleteModuleAsync(int id);

    }
}
