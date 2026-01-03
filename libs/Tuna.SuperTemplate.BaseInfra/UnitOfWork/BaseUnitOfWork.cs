using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuna.SuperTemplate.BaseInfra.Entities.Interface;
using Tuna.SuperTemplate.BaseInfra.Repositories.Interfaces;
using Tuna.SuperTemplate.BaseInfra.UnitOfWork.Interfaces;

namespace Tuna.SuperTemplate.BaseInfra.UnitOfWork;

public abstract class BaseUnitOfWork<TContext> :IBaseUnitOfWork     where TContext : DbContext
{
    public string ContextId { get; } 
    protected readonly TContext _context;
    protected readonly ConcurrentDictionary<string, object> _repos;
    protected readonly IServiceProvider _serviceProvider;

    protected BaseUnitOfWork(IDbContextFactory<TContext> contextFactory, IServiceProvider serviceProvider)
    {
        ContextId = _context.ContextId.InstanceId.ToString();
        _context = contextFactory.CreateDbContext();
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _repos = new ConcurrentDictionary<string, object>();
    }

    public async Task<int> SaveChangesAsync(bool isTransactionalData)
    {
        int result = 0;
        if (!isTransactionalData)
        {
            result = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return result;
        }
        await _context.Database.BeginTransactionAsync();

        try
        {
            result  = await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await _context.Database.RollbackTransactionAsync();
        }
        _context.ChangeTracker.Clear();
        return result;
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _context is not null)
        {
            _context.Dispose();
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if(_context is not null)
        {
            await _context.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
    protected IRepository<T> GetRepository<T>() where T : class,IEntity
    {
        string key = typeof(T).FullName;
        if(!_repos.TryGetValue(key,out object repo))
        {
            repo = _serviceProvider.GetRequiredService<IRepository<T>>().SetDbContext(_context);
            _repos.TryAdd(key, repo);
        }
        return (IRepository<T>)repo;
    }
}
