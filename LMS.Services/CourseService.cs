using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;

namespace LMS.Services
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync(bool includeModules = false, bool trackChanges = false)
        {
            var query = _unitOfWork.CourseRepository.FindAll(trackChanges);

            if (includeModules)
            {
                query = query.Include(c => c.Modules);
            }

            return await query.ToListAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(int id, bool includeModules = false, bool trackChanges = false)
        {
            var query = _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == id, trackChanges);

            if (includeModules)
            {
                query = query.Include(c => c.Modules);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Course>> GetCoursesByUserAsync(string userId)
        {
            var query = _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.UserId == userId, trackChanges: false)
                .Include(cu => cu.Course)
                .Select(cu => cu.Course);

            return await query.ToListAsync();
        }

        public async Task CreateCourseAsync(Course course)
        {
            _unitOfWork.CourseRepository.Create(course);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateCourseAsync(Course course)
        {
            _unitOfWork.CourseRepository.Update(course);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteCourseAsync(Course course)
        {
            _unitOfWork.CourseRepository.Delete(course);
            await _unitOfWork.CompleteAsync();
        }

        // ------------------------
        // NEW: Enrollment features
        // ------------------------

        public async Task<bool> AssignUserAsync(int courseId, string userId)
        {
            // Ensure course exists
            var courseExists = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == courseId, trackChanges: false)
                .AnyAsync();
            if (!courseExists) return false;

            // Idempotent: if already assigned, do nothing
            var already = await _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId && cu.UserId == userId, trackChanges: true)
                .AnyAsync();
            if (already) return true;

            _unitOfWork.CourseUserRepository.Create(new CourseUser
            {
                CourseId = courseId,
                UserId = userId
            });

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<int> AssignUsersAsync(int courseId, IEnumerable<string> userIds)
        {
            var ids = userIds?.Where(id => !string.IsNullOrWhiteSpace(id))
                              .Select(id => id.Trim())
                              .Distinct(StringComparer.Ordinal)
                              .ToArray() ?? Array.Empty<string>();
            if (ids.Length == 0) return 0;

            var courseExists = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == courseId, trackChanges: false)
                .AnyAsync();
            if (!courseExists) return 0;

            var already = await _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId && ids.Contains(cu.UserId), trackChanges: false)
                .Select(cu => cu.UserId)
                .ToListAsync();

            var toAdd = ids.Except(already, StringComparer.Ordinal).ToList();
            if (toAdd.Count == 0) return 0;

            foreach (var uid in toAdd)
            {
                _unitOfWork.CourseUserRepository.Create(new CourseUser
                {
                    CourseId = courseId,
                    UserId = uid
                });
            }

            await _unitOfWork.CompleteAsync();
            return toAdd.Count;
        }

        public async Task<bool> UnassignUserAsync(int courseId, string userId)
        {
            var cu = await _unitOfWork.CourseUserRepository
                .FindByCondition(x => x.CourseId == courseId && x.UserId == userId, trackChanges: true)
                .FirstOrDefaultAsync();

            if (cu is null) return false;

            _unitOfWork.CourseUserRepository.Delete(cu);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> IsUserInCourseAsync(int courseId, string userId)
        {
            return await _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId && cu.UserId == userId, trackChanges: false)
                .AnyAsync();
        }

        public async Task<IEnumerable<UserDto>> GetUsersForCourseAsync(int courseId)
        {
            var query = _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId, trackChanges: false)
                .Include(cu => cu.User) // ApplicationUser navigation
                .Select(cu => new UserDto
                {
                    Id = cu.User.Id,
                    UserName = cu.User.UserName,
                    Email = cu.User.Email,
                    FullName = null
                });

            return await query.ToListAsync();
        }


    }
}