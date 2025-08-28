using AutoMapper;
using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
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
        private readonly IMapper _mapper;
        public ModuleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ModuleDto>> GetAllModulesAsync(int courseId, bool includeActivities = false, bool trackChanges = false)
        {
            var modules = await _unitOfWork.ModuleRepository.GetByCourseAsync(courseId, includeActivities, trackChanges);
            return _mapper.Map<IEnumerable<ModuleDto>>(modules);
        }

        public async Task<ModuleDto?> GetModuleByIdAsync(int id, bool includeActivities = false,  bool trackChanges = false)
        {
            var module = await _unitOfWork.ModuleRepository.GetByIdAsync(id, includeActivities, trackChanges);
            return _mapper.Map<ModuleDto?>(module);
        }
        public async Task<ModuleDto> CreateModuleAsync(int courseId, ModuleCreateDto dto)
        {
            var module = _mapper.Map<Module>(dto);
            module.CourseId = courseId;
            _unitOfWork.ModuleRepository.Create(module);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ModuleDto>(module);
        }

        public async Task<bool> UpdateModuleAsync(int id, ModuleUpdateDto dto)
        {
            var module = await _unitOfWork.ModuleRepository.GetByIdAsync(id);
            if (module == null)
                throw new ArgumentNullException(nameof(module), "Module not found");

            _mapper.Map(dto, module);
            _unitOfWork.ModuleRepository.Update(module);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteModuleAsync(int id)
        {
            var module = await _unitOfWork.ModuleRepository.GetByIdAsync(id);
            if (module == null)
                throw new ArgumentNullException(nameof(module), "Module not found");

            _unitOfWork.ModuleRepository.Delete(module);
            await _unitOfWork.CompleteAsync();
            return true;
        }
    }
}