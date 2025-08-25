using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Services
{
    public class ModuleService : IModuleService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ModuleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Module>> GetAllModulesAsync(bool trackChanges = false)
        {
            return await _unitOfWork.ModuleRepository.GetAllAsync(trackChanges);
        }

        public Task<Module?> GetModuleByIdAsync(int id, bool trackChanges = false)
        {
            throw new NotImplementedException();
        }
        public Task CreateModuleAsync(Module module)
        {
            throw new NotImplementedException();
        }

        public Task UpdateModuleAsync(Module module)
        {
            throw new NotImplementedException();
        }

        public Task DeleteModuleAsync(Module module)
        {
            throw new NotImplementedException();
        }
    }
}
