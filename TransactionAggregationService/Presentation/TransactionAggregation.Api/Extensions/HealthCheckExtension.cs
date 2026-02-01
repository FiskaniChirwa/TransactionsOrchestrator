using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using TransactionAggregation.Api.HealthChecks;

namespace TransactionAggregation.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ExternalApiHealthCheck>("external_apis", tags: ["dependencies"])
            .AddCheck<CacheHealthCheck>("cache", tags: ["infrastructure"])
            .AddCheck<OutboxHealthCheck>("outbox");

        return services;
    }
}