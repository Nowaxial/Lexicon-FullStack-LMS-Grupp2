using Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IModuleService
    {

        Task<IEnumerable<Module>> GetAllModulesAsync(bool trackChanges = false);
        Task<Module?> GetModuleByIdAsync(int id, bool trackChanges = false);

        Task CreateModuleAsync(Module module);
        Task UpdateModuleAsync(Module module);
        Task DeleteModuleAsync(Module module);

    }
}
