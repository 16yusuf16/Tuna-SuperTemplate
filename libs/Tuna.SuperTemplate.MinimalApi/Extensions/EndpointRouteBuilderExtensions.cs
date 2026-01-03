using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tuna.SuperTemplate.MinimalApi.Helper;
using Tuna.SuperTemplate.MinimalApi.Interfaces;
using Tuna.SuperTemplate.MinimalApi.Models;
namespace Tuna.SuperTemplate.MinimalApi.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapCommandEndpoint<TRequest, TCommand>(
        this IEndpointRouteBuilder builder,
        string pattern,
        Func<TRequest, TCommand> mapRequestToCommand
    ) where TRequest : class where TCommand : IRequest
    {
        return builder.MapPost(pattern, async ([AsParameters] HttpCommand<TRequest> parameters) =>
        {
            var command = mapRequestToCommand(parameters.Request);
            await parameters.Mediator.Send(command, parameters.CancellationToken);
            return Results.NoContent();
        })
        .WithName(typeof(TCommand).Name)
        .WithDisplayName(typeof(TCommand).ToReadableName())
        .WithSummary(typeof(TCommand).ToReadableName())
        .WithDescription(typeof(TCommand).ToReadableName());
    }

    public static RouteHandlerBuilder MapQueryEndpoint<TRequestParameters, TResponse, TQuery, TQueryResult>(
        this IEndpointRouteBuilder builder,
        string pattern,
        Func<TRequestParameters, TQuery> mapRequestToQuery,
        Func<TQueryResult, TResponse> mapQueryResultToResponse
    ) where TRequestParameters : IHttpQuery
      where TResponse : class
      where TQueryResult : class
      where TQuery : IRequest<TQueryResult>
    {
        return builder.MapGet(pattern, async ([AsParameters] TRequestParameters parameters) =>
        {
            var query = mapRequestToQuery(parameters);
            var result = await parameters.Mediator.Send(query, parameters.CancellationToken);
            var response = mapQueryResultToResponse(result);
            return Results.Ok(response);
        })
        .WithName(typeof(TQuery).Name)
        .WithDisplayName(typeof(TQuery).ToReadableName())
        .WithSummary(typeof(TQuery).ToReadableName())
        .WithDescription(typeof(TQuery).ToReadableName());
    }
}
