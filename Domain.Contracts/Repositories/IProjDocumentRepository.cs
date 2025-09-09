using Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts.Repositories
{
    public interface IProjDocumentRepository : IRepositoryBase<ProjDocument>
    {
        Task<ProjDocument?> GetByIdAsync(int id, bool trackChanges = false);

        Task<IEnumerable<ProjDocument>> GetByCourseAsync(int courseId, bool trackChanges = false);
        Task<IEnumerable<ProjDocument>> GetByModuleAsync(int moduleId, bool trackChanges = false);
        Task<IEnumerable<ProjDocument>> GetByActivityAsync(int activityId, bool trackChanges = false);

        Task<IEnumerable<ProjDocument>> GetByStudentAsync(string studentId, bool trackChanges = false);
    }
}
