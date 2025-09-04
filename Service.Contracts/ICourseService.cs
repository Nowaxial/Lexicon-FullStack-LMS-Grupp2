using LMS.Shared.DTOs.EntitiesDtos;
using UserDto = LMS.Shared.DTOs.UsersDtos.UserDto;

namespace Service.Contracts
{
    public interface ICourseService
    {
        // Reads
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool includeModules = false, bool includeActivities = false, bool trackChanges = false);
        Task<CourseDto?> GetCourseByIdAsync(int id, bool includeModules = false, bool includeActivities = false, bool trackChanges = false);
        Task<IEnumerable<CourseDto>> GetCoursesByUserAsync(string userId, bool includeModules = false, bool includeActivities = false, bool trackChanges = false);

        // Writes
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int id);

        // Enrollments
        Task<bool> AssignUserAsync(int courseId, string userId);
        Task<int> AssignUsersAsync(int courseId, IEnumerable<string> userIds);
        Task<bool> UnassignUserAsync(int courseId, string userId);
        Task<bool> IsUserInCourseAsync(int courseId, string userId);
        Task<IEnumerable<UserDto>> GetUsersForCourseAsync(int courseId);
    }
}
