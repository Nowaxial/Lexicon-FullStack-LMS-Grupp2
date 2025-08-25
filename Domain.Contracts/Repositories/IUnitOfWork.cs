using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository CourseRepository { get; }
        IModuleRepository ModuleRepository { get; }

        Task CompleteAsync();
    }
}