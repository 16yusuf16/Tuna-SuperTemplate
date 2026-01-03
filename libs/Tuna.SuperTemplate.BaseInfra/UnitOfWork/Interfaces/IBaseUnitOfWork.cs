using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuna.SuperTemplate.BaseInfra.UnitOfWork.Interfaces;

public interface IBaseUnitOfWork : IDisposable,IAsyncDisposable
{
    string ContextId { get; }
    Task<int> SaveChangesAsync(bool isTransactionalData);
}
