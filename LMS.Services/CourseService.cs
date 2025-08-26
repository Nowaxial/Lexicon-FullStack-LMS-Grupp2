using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using Service.Contracts;
using Microsoft.EntityFrameworkCore;

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
    }
}