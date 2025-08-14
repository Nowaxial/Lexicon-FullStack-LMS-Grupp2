namespace Domain.Contracts.Repositories;

public interface IUnitOfWork
{
    Task CompleteAsync();
}