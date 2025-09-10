using AutoMapper;
using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos;
using UserDto = LMS.Shared.DTOs.UsersDtos.UserDto;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;



namespace LMS.Services
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CourseService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // ---------- Reads ----------
        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool includeModules = false, bool includeActivities = false, bool trackChanges = false)
        {
            var query = _unitOfWork.CourseRepository.FindAll(trackChanges);
            if (includeModules)
            {
                query = query.Include(c => c.Modules);

                if (includeActivities)
                {
                    query = query.Include(c => c.Modules)
                                 .ThenInclude(m => m.Activities);
                }

            }

            var courses = await query.ToListAsync();
            return _mapper.Map<IEnumerable<CourseDto>>(courses);
        }

        public async Task<CourseDto?> GetCourseByIdAsync(int id, bool includeModules = false, bool includeActivities = false, bool trackChanges = false)
        {
            var query = _unitOfWork.CourseRepository.FindByCondition(c => c.Id == id, trackChanges);
            if (includeModules)
            {
                query = query.Include(c => c.Modules);

                if (includeActivities)
                {
                    query = query.Include(c => c.Modules)
                                 .ThenInclude(m => m.Activities);
                }
            }

            var course = await query.FirstOrDefaultAsync();
            return course is null ? null : _mapper.Map<CourseDto>(course);
        }

        public async Task<IEnumerable<CourseDto>> GetCoursesByUserAsync(string userId, bool includeModules = false, bool includeActivities = false, bool trackChanges = false)
        {
            var query = _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.UserId == userId, false)
                .Include(cu => cu.Course)
                .Select(cu => cu.Course);

            if (!trackChanges)
                query = query.AsNoTracking();

            if (includeModules)
                query = query.Include(c => c.Modules);

            var courses = await query.ToListAsync();
            return _mapper.Map<IEnumerable<CourseDto>>(courses);
        }

        // ---------- Writes ----------
        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            var course = _mapper.Map<Course>(dto);
            _unitOfWork.CourseRepository.Create(course);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CourseDto>(course);
        }

        public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto)
        {
            var course = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == id, true)
                .FirstOrDefaultAsync();

            if (course is null) return false;

            _mapper.Map(dto, course);
            _unitOfWork.CourseRepository.Update(course);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == id, true)
                .FirstOrDefaultAsync();

            if (course is null) return false;

            _unitOfWork.CourseRepository.Delete(course);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        // ---------- Enrollments ----------
        public async Task<bool> AssignUserAsync(int courseId, string userId)
        {
            var courseExists = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Id == courseId, false)
                .AnyAsync();
            if (!courseExists) return false;

            var already = await _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId && cu.UserId == userId, true)
                .AnyAsync();
            if (already) return true;

            _unitOfWork.CourseUserRepository.Create(new CourseUser { CourseId = courseId, UserId = userId });
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
                .FindByCondition(c => c.Id == courseId, false)
                .AnyAsync();
            if (!courseExists) return 0;

            var existing = await _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId && ids.Contains(cu.UserId), false)
                .Select(cu => cu.UserId)
                .ToListAsync();

            var toAdd = ids.Except(existing, StringComparer.Ordinal).ToList();
            foreach (var uid in toAdd)
                _unitOfWork.CourseUserRepository.Create(new CourseUser { CourseId = courseId, UserId = uid });

            await _unitOfWork.CompleteAsync();
            return toAdd.Count;
        }

        public async Task<bool> UnassignUserAsync(int courseId, string userId)
        {
            var cu = await _unitOfWork.CourseUserRepository
                .FindByCondition(x => x.CourseId == courseId && x.UserId == userId, true)
                .FirstOrDefaultAsync();
            if (cu is null) return false;

            _unitOfWork.CourseUserRepository.Delete(cu);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public Task<bool> IsUserInCourseAsync(int courseId, string userId) =>
            _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId && cu.UserId == userId, false)
                .AnyAsync();

        public async Task<IEnumerable<UserDto>> GetUsersForCourseAsync(int courseId)
        {
            var users = await _unitOfWork.CourseUserRepository
                .FindByCondition(cu => cu.CourseId == courseId, false)
                .Include(cu => cu.User)
                .Select(cu => new UserDto
                {
                    Id = cu.User.Id,
                    FirstName = cu.User.FirstName,
                    LastName = cu.User.LastName,
                    UserName = cu.User.UserName,
                    Email = cu.User.Email,
                    IsTeacher = cu.IsTeacher
                })
                .ToListAsync();

            return users;
        }
        public async Task<IEnumerable<CourseDto>> SearchCoursesAsync(string query, bool trackChanges = false)
        {
            var courses = await _unitOfWork.CourseRepository
                .FindByCondition(c => c.Name.Contains(query) || (c.Description ?? "").Contains(query), trackChanges)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDto>>(courses);
        }
    }
}
