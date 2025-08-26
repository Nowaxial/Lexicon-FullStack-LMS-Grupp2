using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository CourseRepository { get; }
        IModuleRepository ModuleRepository { get; }

        IProjActivityRepository ProjActivityRepository { get; }

        Task CompleteAsync();
    }
}