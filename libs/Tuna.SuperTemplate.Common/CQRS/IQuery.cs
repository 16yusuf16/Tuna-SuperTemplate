using MediatR;

namespace Tuna.SuperTemplate.Common.CQRS;

public interface IQuery<out T> : IRequest<T>
    where T : notnull
{
}