using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface ICourseUserRepository : IRepositoryBase<CourseUser>, IInternalRepositoryBase<CourseUser>
    {
        //custom methods here

        Task<IEnumerable<string>> GetTeacherUserIdsByCourseAsync(int courseId, bool trackChanges = false);

    }
}