using Domain.Models.Entities;
using LMS.Shared.DTOs.EntitiesDtos;

namespace Service.Contracts
{
    public interface ICourseService
    {
        Task<IEnumerable<Course>> GetAllCoursesAsync(bool includeModules = false, bool trackChanges = false);
        Task<Course?> GetCourseByIdAsync(int id, bool includeModules = false, bool trackChanges = false);
        Task<IEnumerable<Course>> GetCoursesByUserAsync(string userId);

        Task CreateCourseAsync(Course course);
        Task UpdateCourseAsync(Course course);
        Task DeleteCourseAsync(Course course);

        // NEW
        Task<bool> AssignUserAsync(int courseId, string userId);
        Task<int> AssignUsersAsync(int courseId, IEnumerable<string> userIds); // optional bulk
        Task<bool> UnassignUserAsync(int courseId, string userId);
        Task<bool> IsUserInCourseAsync(int courseId, string userId);
        Task<IEnumerable<UserDto>> GetUsersForCourseAsync(int courseId);
    }
}