using Domain.Models.Entities;

namespace Domain.Contracts.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository CourseRepository { get; }

        IProjActivityRepository ProjActivityRepository { get; }

        Task CompleteAsync();
    }
}