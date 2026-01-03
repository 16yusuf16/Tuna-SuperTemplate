using MediatR;
using Microsoft.AspNetCore.Http;

namespace Tuna.SuperTemplate.MinimalApi.Models;

public sealed record HttpCommand<TRequest>(
    TRequest Request,
    HttpContext Context,
    IMediator Mediator,
    CancellationToken CancellationToken
);
