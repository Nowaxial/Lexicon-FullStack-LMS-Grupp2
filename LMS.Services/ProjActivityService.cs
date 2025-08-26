using AutoMapper;
using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;

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

        public async Task<ProjActivityDto> CreateActivityAsync(CreateProjActivityDto dto)
        {
            var activity = _mapper.Map<ProjActivity>(dto);
            _unitOfWork.ProjActivityRepository.Create(activity);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ProjActivityDto>(activity);
        }

        public async Task UpdateActivityAsync(int id, UpdateProjActivityDto dto)
        {
            var activity = await _unitOfWork.ProjActivityRepository
                .FindByCondition(a => a.Id == id, trackChanges: true)
                .FirstOrDefaultAsync();

            if (activity != null)
            {
                _mapper.Map(dto, activity);
                await _unitOfWork.CompleteAsync();
            }
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
