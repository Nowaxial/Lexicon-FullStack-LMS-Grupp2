using System.Linq.Expressions;

namespace Domain.Contracts.Repositories;

public interface IInternalRepositoryBase<T>
{
    IQueryable<T> FindAll(bool trackChanges = false);
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);
}
