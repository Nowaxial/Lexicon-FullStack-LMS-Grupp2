using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LMS.Infractructure.Repositories;
public abstract class RepositoryBase<T> : IRepositoryBase<T>, IInternalRepositoryBase<T> where T : class //Do Entitybase
{
    protected DbSet<T> DbSet { get; }

    public RepositoryBase(ApplicationDbContext context)
    {
        DbSet = context.Set<T>();
    }

    public IQueryable<T> FindAll(bool trackChanges = false) =>
        !trackChanges ? DbSet.AsNoTracking() :
                        DbSet;

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false) =>
        !trackChanges ? DbSet.Where(expression).AsNoTracking() :
                        DbSet.Where(expression);

    public void Create(T entity) => DbSet.Add(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Delete(T entity) => DbSet.Remove(entity);
}
