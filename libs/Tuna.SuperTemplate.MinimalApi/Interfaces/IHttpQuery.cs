using MediatR;

namespace Tuna.SuperTemplate.MinimalApi.Interfaces;

public interface IHttpQuery
{
    CancellationToken CancellationToken { get; }
    IMediator Mediator { get; }
}
