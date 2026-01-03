namespace Tuna.SuperTemplate.BaseInfra.Entities.Interface;

public interface IEntity;

public interface IEntity<T> : IEntity
{
    T Id { get; set; }
}
