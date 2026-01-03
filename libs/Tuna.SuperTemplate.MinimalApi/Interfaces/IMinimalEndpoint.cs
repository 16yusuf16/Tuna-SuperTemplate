namespace Tuna.SuperTemplate.MinimalApi.Interfaces;

public interface IMinimalEndpoint
{
    string GroupName { get; }
    string PrefixRoute { get; }
    string Version { get; }
    void MapEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder);
}