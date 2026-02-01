using Document.Infrastructure.Pdf;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Document.Api.HealthChecks;

public class PdfGenerationHealthCheck : IHealthCheck
{
    private readonly IPdfTemplateRegistry _templateRegistry;
    private readonly ILogger<PdfGenerationHealthCheck> _logger;

    public PdfGenerationHealthCheck(
        IPdfTemplateRegistry templateRegistry,
        ILogger<PdfGenerationHealthCheck> logger)
    {
        _templateRegistry = templateRegistry;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if templates are registered
            var supportedTypes = _templateRegistry.GetSupportedTypes();

            if (supportedTypes.Count == 0)
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded("No PDF templates registered"));
            }

            var data = new Dictionary<string, object>
            {
                ["templateCount"] = supportedTypes.Count,
                ["supportedTypes"] = string.Join(", ", supportedTypes)
            };

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    $"{supportedTypes.Count} PDF template(s) registered",
                    data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation health check failed");
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "PDF template registry is not accessible",
                    ex));
        }
    }
}