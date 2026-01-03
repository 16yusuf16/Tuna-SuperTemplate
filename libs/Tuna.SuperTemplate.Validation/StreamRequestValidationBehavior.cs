using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Tuna.SuperTemplate.Logging;

namespace Tuna.SuperTemplate.Validation;

public class StreamRequestValidationBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
    where TResponse : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppLogger<StreamRequestValidationBehavior<TRequest, TResponse>> _logger;

    public StreamRequestValidationBehavior(
        IServiceProvider serviceProvider,
        AppLogger<StreamRequestValidationBehavior<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<TResponse> Handle(
        TRequest message,
        StreamHandlerDelegate<TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            await foreach (var response in next())
            {
                yield return response;
            }

            yield break;
        }

        _logger.LogInformation(
            "[{Prefix}] Handle request={RequestData} and response={ResponseData}",
            nameof(StreamRequestValidationBehavior<TRequest, TResponse>),
            typeof(TRequest).Name,
            typeof(TResponse).Name
        );

        _logger.LogDebug(
            "Handling {FullName} with content {Request}",
            typeof(TRequest).FullName ?? typeof(TRequest).Name,
            JsonSerializer.Serialize(message)
        );

        var validationResult = await validator.ValidateAsync(message, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await foreach (var response in next())
        {
            yield return response;
            _logger.LogInformation("Handled {FullName}", typeof(TRequest).FullName ?? typeof(TRequest).Name);
        }
    }
}