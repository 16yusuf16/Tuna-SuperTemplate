using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;
using Tuna.SuperTemplate.BaseInfra.Entities.Interface;
using Tuna.SuperTemplate.BaseInfra.Repositories.Interfaces;

namespace Tuna.SuperTemplate.BaseInfra.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class,IEntity
{
    private DbSet<TEntity> _dbSet;
    public string ContextId { get; private set; }
   

    public async Task<TEntity> GetAsync(object id, CancellationToken cancellationToken = default)
    {
        return await GetAll().FirstOrDefaultAsync(ExpressionForId(id), cancellationToken);
    }

    public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().AsQueryable().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public IQueryable<TEntity> GetAll()
    {
        return _dbSet.AsNoTracking().AsQueryable();
    }

    public async Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        EntityEntry<TEntity> entry = await  _dbSet.AddAsync(entity,cancellationToken);
        return entry.State == EntityState.Added;
    }

    public async Task<bool> AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        return true;
    }

    public bool Remove(TEntity entity)
    {
        EntityEntry<TEntity> entry =  _dbSet.Remove(entity);
        return entry.State == EntityState.Deleted;
    }

    public bool RemoveRange(List<TEntity> entities)
    {
       _dbSet.RemoveRange(entities);
        return true;
    }

    public bool Update(TEntity entity)
    {
        EntityEntry<TEntity> entry = _dbSet.Update(entity);
        return entry.State == EntityState.Modified;
    }

    public void UpdateRange(List<TEntity> entities)
    {
       _dbSet.UpdateRange(entities);
    }
    private static Expression<Func<TEntity, bool>> ExpressionForId(object id)
    {
        var param = Expression.Parameter(typeof(TEntity));
        var body = Expression.Equal(
            Expression.PropertyOrField(param, "Id"),
            Expression.Constant(id)
        );
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    public IRepository<TEntity> SetDbContext<T>(T dbContext) where T : DbContext
    {
        _dbSet = dbContext.Set<TEntity>();
        ContextId = dbContext.ContextId.InstanceId.ToString();
        return this;
    }
}
