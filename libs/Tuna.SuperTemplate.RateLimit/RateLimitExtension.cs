using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Threading.RateLimiting;

namespace Tuna.SuperTemplate.RateLimit;

public static class RateLimitExtension
{
    private const string ForwardedForHeader = "X-Forwarded-For";
    private static readonly HttpStatusCode TooManyRequests = HttpStatusCode.TooManyRequests;

    public static IServiceCollection AddCustomRateLimit(this IServiceCollection services, IConfiguration configuration)

    {
        var section = configuration.GetSection(nameof(RateLimitOptions));
        var rateLimitOptions = section.Get<RateLimitOptions>();
        if (rateLimitOptions is null)
        {
            throw new InvalidOperationException($"Configuration section '{nameof(RateLimitOptions)}' is missing or invalid.");
        }

        services.Configure<RateLimitOptions>(section);

        var permitLimit = rateLimitOptions.Limit;
        var queueLimit = rateLimitOptions.QueueLimit;
        var window = TimeSpan.FromMilliseconds(rateLimitOptions.PeriodInMs);


        services.AddRateLimiter(opt =>
        {
            opt.OnRejected = (OnRejectedContext context, CancellationToken _) =>
            {
                var problemDetailsService =
                    context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

                var problemDetails = new ProblemDetails
                {
                    Status = (int)TooManyRequests,
                    Title = TooManyRequests.ToString(),
                };

                context.HttpContext.Response.StatusCode = (int)TooManyRequests;
                return problemDetailsService.WriteAsync(
                    new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problemDetails }
                );
            };

            opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                // Avoid repeated allocations for partition key creation
                var clientIp = httpContext.GetClientIp() ?? "N/A";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = queueLimit
                    }
                );
            });
        });

        return services;
    }

    private static string? GetClientIp(this HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(ForwardedForHeader, out var values) && values.Count > 0)
        {
            var header = values[0];
            if (string.IsNullOrEmpty(header))
                return null;

            var commaIndex = header.IndexOf(',');
            if (commaIndex <= 0)
                return header.Trim();

            return header.Substring(0, commaIndex).Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}