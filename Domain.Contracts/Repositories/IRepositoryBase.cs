namespace Domain.Contracts.Repositories;
public interface IRepositoryBase<T> : IInternalRepositoryBase<T> where T : class
{
    void Create(T entity);
    void Update(T entity);
    void Delete(T entity);

}
