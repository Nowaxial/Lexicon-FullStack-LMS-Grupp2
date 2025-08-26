using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Infractructure.Data;

namespace LMS.Infractructure.Repositories
{
    public class CourseUserRepository : RepositoryBase<CourseUser>, ICourseUserRepository
    {
        public CourseUserRepository(ApplicationDbContext context) : base(context) { }
    }
}
