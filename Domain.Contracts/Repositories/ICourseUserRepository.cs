using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface ICourseUserRepository : IRepositoryBase<CourseUser>, IInternalRepositoryBase<CourseUser>
    {
        //custom methods here
    }
}