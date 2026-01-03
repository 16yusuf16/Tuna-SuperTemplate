using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Tuna.SuperTemplate.ApiDocs.Extensions;
using Tuna.SuperTemplate.Cors;
using Tuna.SuperTemplate.Exception.Extension;
using Tuna.SuperTemplate.HealthCheck;
using Tuna.SuperTemplate.HybridCache.Extensions;
using Tuna.SuperTemplate.HybridCache.Hybrid;
using Tuna.SuperTemplate.HybridCache.Redis;
using Tuna.SuperTemplate.Logging;
using Tuna.SuperTemplate.MinimalApi.Extensions;
using Tuna.SuperTemplate.OpenApiDocs.Extensions;
using Tuna.SuperTemplate.RateLimit;
using Tuna.SuperTemplate.Resilience.Configuration;
using Tuna.SuperTemplate.Resilience.Extensions;
using Tuna.SuperTemplate.Security.Extensions;
using Tuna.SuperTemplate.Security.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseCentralizedLogging();

builder.Services.AddCentralizedLogging(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddOpenApiDocs("Tuna SuperTemplate API", "v1");

builder.Services.Configure<ResilienceOptions>(builder.Configuration.GetSection("Resilience"));

//redis
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(nameof(CacheSettings)));
builder.Services.TryAddSingleton<CacheSettings>(sp => sp.GetRequiredService<IOptions<CacheSettings>>().Value);

builder.Services.Configure<RedisServerSettings>(builder.Configuration.GetSection(nameof(RedisServerSettings)));
builder.Services.TryAddSingleton<RedisServerSettings>(sp => sp.GetRequiredService<IOptions<RedisServerSettings>>().Value);
builder.Services.AddRedisCache();
builder.Services.UseCaching(builder.Configuration);

builder.Services.AddCustomRateLimit(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt settings are missing!");

var encryptionKey = builder.Configuration["Encryption:Key"]
    ?? throw new InvalidOperationException("Encryption key is missing!");

builder.Services
    .AddSuperSecurity(jwtOptions, encryptionKey)
    .AddSuperJwtAuthentication(jwtOptions);

// Mediatr
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));

// Minimal Endpoints
builder.Services.AddMinimalEndpoints(typeof(Program).Assembly);

builder.Services.AddResiliencePolicies();
builder.Services.AddHttpClient();

builder.Services.AddDefaultCors(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApiDocs();
}
app.UseCustomHealthCheck();
app.UseCustomProblemDetails();
app.UseHttpsRedirection();


app.UseDefaultCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapMinimalEndpoints();
app.MapControllers();

app.Run();
