using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Tuna.SuperTemplate.BaseInfra.Entities.Interface;

namespace Tuna.SuperTemplate.BaseInfra.Repositories.Interfaces;

public interface IRepository<TEntity> where TEntity:class,IEntity
{
    public string ContextId { get; }
    Task<TEntity> GetAsync(object id, CancellationToken cancellationToken = default);
    Task<TEntity> GetAsync(Expression<Func<TEntity,bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<TEntity> GetAll();
    Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default);
    bool Remove(TEntity entity);
    bool RemoveRange(List<TEntity> entities);
    bool Update(TEntity entity);
    void UpdateRange(List<TEntity> entities);
    IRepository<TEntity> SetDbContext<T>(T dbContext) where T :DbContext;

}
