using AutoMapper;
using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System.ComponentModel.DataAnnotations;

namespace LMS.Services
{
    public class ProjActivityService : IProjActivityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProjActivityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProjActivityDto>> GetAllActivitiesAsync(bool trackChanges = false)
        {
            var activities = await _unitOfWork.ProjActivityRepository.FindAll(trackChanges).ToListAsync();
            return _mapper.Map<IEnumerable<ProjActivityDto>>(activities);
        }

        public async Task<ProjActivityDto?> GetActivityByIdAsync(int id, bool trackChanges = false)
        {
            var activity = await _unitOfWork.ProjActivityRepository
                .FindByCondition(a => a.Id == id, trackChanges)
                .FirstOrDefaultAsync();
            return activity == null ? null : _mapper.Map<ProjActivityDto>(activity);
        }

        public async Task<IEnumerable<ProjActivityDto>> GetActivitiesByModuleIdAsync(int moduleId, bool trackChanges = false)
        {
            var activities = await _unitOfWork.ProjActivityRepository.GetActivitiesByModuleIdAsync(moduleId, trackChanges);
            return _mapper.Map<IEnumerable<ProjActivityDto>>(activities);
        }

        public async Task<ProjActivityDto> CreateActivityAsync(int moduleId, CreateProjActivityDto dto)
        {
            var module = await _unitOfWork.ModuleRepository
                .FindByCondition(m => m.Id == moduleId, trackChanges: false)
                .FirstOrDefaultAsync();

            if (module == null)
                throw new InvalidOperationException($"Module with id {moduleId} not found");

            // ✅ Business validation
            if (dto.Starts > dto.Ends)
                throw new ValidationException("Activity start date must be before end date");

            if (dto.Starts < module.Starts.ToDateTime(TimeOnly.MinValue) ||
                dto.Ends > module.Ends.ToDateTime(TimeOnly.MaxValue))
            {
                throw new ValidationException(
                    $"Activity must fit within module period {module.Starts:d} – {module.Ends:d}");
            }

            var activity = _mapper.Map<ProjActivity>(dto);
            activity.ModuleId = moduleId;

            _unitOfWork.ProjActivityRepository.Create(activity);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProjActivityDto>(activity);
        }

        public async Task UpdateActivityAsync(int id, UpdateProjActivityDto dto)
        {
            var activity = await _unitOfWork.ProjActivityRepository
                .FindByCondition(a => a.Id == id, trackChanges: true)
                .FirstOrDefaultAsync();

            if (activity == null)
                throw new InvalidOperationException($"Activity with id {id} not found");

            var module = await _unitOfWork.ModuleRepository
                .FindByCondition(m => m.Id == activity.ModuleId, trackChanges: false)
                .FirstOrDefaultAsync();

            if (module == null)
                throw new InvalidOperationException($"Module for activity {id} not found");

            // ✅ Business validation
            if (dto.Starts > dto.Ends)
                throw new ValidationException("Activity start date must be before end date");

            if (dto.Starts < module.Starts.ToDateTime(TimeOnly.MinValue) ||
                dto.Ends > module.Ends.ToDateTime(TimeOnly.MaxValue))
            {
                throw new ValidationException(
                    $"Activity must fit within module period {module.Starts:d} – {module.Ends:d}");
            }

            // ✅ Apply changes
            _mapper.Map(dto, activity);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteActivityAsync(int id)
        {
            var activity = await _unitOfWork.ProjActivityRepository
                .FindByCondition(a => a.Id == id, trackChanges: false)
                .FirstOrDefaultAsync();

            if (activity != null)
            {
                _unitOfWork.ProjActivityRepository.Delete(activity);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}
