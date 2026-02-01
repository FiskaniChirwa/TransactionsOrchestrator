using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TransactionAggregation.Api.Routes;

public static class HealthRoutes
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                service = "TransactionAggregation.API",
                checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        data = entry.Value.Data
                    }
                )
            };

            var statusCode = report.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                _ => StatusCodes.Status503ServiceUnavailable
            };

            return Results.Json(response, statusCode: statusCode);
        })
        .WithName("HealthCheck")
        .WithTags("Health")
        .WithOpenApi();

        app.MapGet("/health/detailed", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                service = "TransactionAggregation.API",
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data
                })
            };

            var statusCode = report.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                _ => StatusCodes.Status503ServiceUnavailable
            };

            return Results.Json(response, statusCode: statusCode);
        })
        .WithName("DetailedHealthCheck")
        .WithTags("Health")
        .WithOpenApi();

        app.MapGet("/health/live", () => Results.Ok(new
        {
            status = "Alive",
            timestamp = DateTime.UtcNow
        }))
        .WithName("LivenessProbe")
        .WithTags("Health")
        .ExcludeFromDescription();

        app.MapGet("/health/ready", async Task<IResult> (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();

            if (report.Status == HealthStatus.Healthy)
            {
                return Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
            }

            return Results.Problem(
                 title: "Service Unavailable",
                 detail: "Service is not ready",
                 statusCode: StatusCodes.Status503ServiceUnavailable
             );
        })
        .WithName("ReadinessProbe")
        .WithTags("Health")
        .ExcludeFromDescription();
    }
}