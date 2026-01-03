using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuna.SuperTemplate.BaseInfra.Entities.Interface;

namespace Tuna.SuperTemplate.BaseInfra.Entities;

public abstract  class BaseEntity<T> :IEntity<T> where T : struct
{
    public T Id { get; set; }
}

public abstract class BaseEntity 
{
    public int Id { get; set; }
}

public abstract class BaseWithAuditEntity<T> : BaseAuditEntity, IEntity<T> where T : struct
{
    public T Id { get; set; }
}