using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Tuna.SuperTemplate.Logging;

namespace Tuna.SuperTemplate.Validation;

public class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppLogger<RequestValidationBehavior<TRequest, TResponse>> _logger;

    public RequestValidationBehavior(
        IServiceProvider serviceProvider,
        AppLogger<RequestValidationBehavior<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(
        TRequest message,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await next();
        }

        _logger.LogInformation(
            "[{Prefix}] Handle request={RequestData} and response={ResponseData}",
            nameof(RequestValidationBehavior<TRequest, TResponse>),
            typeof(TRequest).Name,
            typeof(TResponse).Name
        );

        _logger.LogDebug(
            "Handling {FullName} with content {Request}",
            typeof(TRequest).FullName ?? typeof(TRequest).Name,
            JsonSerializer.Serialize(message)
        );

        var validationResult = await validator.ValidateAsync(message, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var response = await next();

        _logger.LogInformation("Handled {FullName}", typeof(TRequest).FullName ?? typeof(TRequest).Name);
        return response;
    }
}
