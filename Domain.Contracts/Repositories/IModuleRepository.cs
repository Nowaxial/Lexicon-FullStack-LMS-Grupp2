using Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts.Repositories
{
    public interface IModuleRepository : IRepositoryBase<Module>
    {
        Task<IEnumerable<Module>> GetAllAsync(int courseId, bool trackChanges = false);
        Task<Module?> GetByIdAsync(int id, bool trackChanges = false);
        Task<IEnumerable<Module>> GetByCourseAsync(int courseId, bool trackChanges = false);
    }
}
