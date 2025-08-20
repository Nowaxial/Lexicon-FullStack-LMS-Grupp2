using Domain.Models.Entities;

namespace Service.Contracts;

public interface ICourseService
{
    Task CreateCourseAsync(Course course);
    Task DeleteCourseAsync(Course course);
    Task<IEnumerable<Course>> GetAllCoursesAsync(bool trackChanges = false);
    Task<Course?> GetCourseByIdAsync(int id, bool trackChanges = false);
    Task UpdateCourseAsync(Course course);
}