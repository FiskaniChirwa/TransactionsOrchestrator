using Document.Api.HealthChecks;
using Document.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Document.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDocumentHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database health check
            .AddDbContextCheck<DocumentDbContext>(
                name: "database",
                tags: new[] { "db", "sql", "sqlite" })
            
            // Storage health check
            .AddCheck<DocumentStorageHealthCheck>(
                name: "storage",
                tags: new[] { "storage", "filesystem" })
            
            // PDF generation health check
            .AddCheck<PdfGenerationHealthCheck>(
                name: "pdf-generation",
                tags: new[] { "pdf", "templates" });

        // Health Checks UI (optional - provides a dashboard)
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("Document Service", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }

    public static IApplicationBuilder UseDocumentHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecks("/health/db", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecks("/health/storage", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("storage"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}