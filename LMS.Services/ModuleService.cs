using AutoMapper;
using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using Microsoft.EntityFrameworkCore;
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
            //  Rule: Starts must be before Ends
            if (dto.Starts > dto.Ends)
                throw new ArgumentException("Module start date must be before the end date.");

            //  Ensure course exists
            var course = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == courseId)
                .FirstOrDefaultAsync();

            if (course == null)
                throw new ArgumentException("Course not found.");

            //  Rule: Module must fit within course dates
            if (dto.Starts < course.Starts || dto.Ends > course.Ends)
                throw new ArgumentException(
                    $"Module dates must be within the course period {course.Starts:d} – {course.Ends:d}"
                );

            var module = _mapper.Map<Module>(dto);
            module.CourseId = courseId;

            _unitOfWork.ModuleRepository.Create(module);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ModuleDto>(module);
        }

        public async Task<bool> UpdateModuleAsync(int id, ModuleUpdateDto dto)
        {
            var module = await _unitOfWork.ModuleRepository
                .FindByCondition(m => m.Id == id, trackChanges: true)
                .FirstOrDefaultAsync();

            if (module == null)
                throw new ArgumentNullException(nameof(module), "Module not found");

            //  Rule: Starts must be before Ends
            if (dto.Starts > dto.Ends)
                throw new ArgumentException("Module start date must be before the end date.");

            //  Ensure course exists for validation
            var course = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == dto.CourseId)
                .FirstOrDefaultAsync();

            if (course == null)
                throw new ArgumentException("Course not found.");

            // Rule: Must fit inside course dates
            if (dto.Starts < course.Starts || dto.Ends > course.Ends)
                throw new ArgumentException(
                    $"Module dates must be within the course period {course.Starts:d} – {course.Ends:d}"
                );

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