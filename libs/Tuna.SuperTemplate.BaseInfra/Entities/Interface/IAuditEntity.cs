namespace Tuna.SuperTemplate.BaseInfra.Entities.Interface;

public interface IAuditEntity
{
    DateTimeOffset? CreateAt { get; set; }
    int? CreateBy { get; set; }
    DateTimeOffset? UpdateAt { get; set; }
    int? UpdateBy { get; set; }
    bool IsDeleted { get; set; } 
}
