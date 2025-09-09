using Domain.Contracts.Repositories;
using Domain.Models.Entities;
using LMS.Infractructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infractructure.Repositories
{
    public class CourseUserRepository : RepositoryBase<CourseUser>, ICourseUserRepository
    {
        public CourseUserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<string>> GetTeacherUserIdsByCourseAsync(int courseId, bool trackChanges = false)
        {
            return await FindByCondition(cu => cu.CourseId == courseId && cu.IsTeacher, trackChanges)
                .Select(cu => cu.UserId)
                .ToListAsync();
        }
    }
}
