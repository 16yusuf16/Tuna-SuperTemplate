using Tuna.SuperTemplate.BaseInfra.Entities.Interface;

namespace Tuna.SuperTemplate.BaseInfra.Entities;

public abstract class BaseAuditEntity : IAuditEntity
{
    public DateTimeOffset? CreateAt { get; set; }
    public int? CreateBy { get; set; }
    public DateTimeOffset? UpdateAt { get; set; }
    public int? UpdateBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}
