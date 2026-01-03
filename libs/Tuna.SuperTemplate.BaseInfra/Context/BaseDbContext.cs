using Microsoft.EntityFrameworkCore;
using Tuna.SuperTemplate.BaseInfra.Entities.Interface;

namespace Tuna.SuperTemplate.BaseInfra.Context;

public class BaseDbContext :DbContext
{
    public BaseDbContext(DbContextOptions options) : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureEntityType();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureEntityType();
        return base.SaveChangesAsync(cancellationToken);
    }
    public override int SaveChanges()
    {
        EnsureEntityType();
        return base.SaveChanges();
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureEntityType();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    private void EnsureEntityType()
    {
        var entities = ChangeTracker.Entries().Where(x =>
        (x.Entity is IAuditEntity) && 
        (x.State == EntityState.Added || x.State == EntityState.Modified || x.State == EntityState.Deleted));
        
        foreach (var entityEntry in entities)
        {
            if(entityEntry.State == EntityState.Deleted)
            {
                entityEntry.State = EntityState.Modified;
            }
            if(entityEntry.Entity is IAuditEntity auditEntity)
            {
                var now = DateTimeOffset.UtcNow;
                switch (entityEntry.State)
                {

                    case EntityState.Modified:
                        auditEntity.UpdateAt = now;
                        break;
                    case EntityState.Added:
                        auditEntity.CreateAt = now;
                        break;

                }
            }
           
        }
    }

    protected BaseDbContext()
    {
    }
}
