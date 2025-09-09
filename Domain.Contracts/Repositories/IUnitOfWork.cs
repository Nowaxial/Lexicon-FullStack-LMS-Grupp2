using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository CourseRepository { get; }
        ICourseUserRepository CourseUserRepository { get; }
        IModuleRepository ModuleRepository { get; }

        IProjActivityRepository ProjActivityRepository { get; }
        IProjDocumentRepository ProjDocumentRepository { get; }

        Task CompleteAsync();
    }
}