using System.Diagnostics;
namespace TransactionAggregation.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items["X-Correlation-Id"]?.ToString();
        
        _logger.LogInformation(
            "Request: {Method} {Path} | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId
        );

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        _logger.LogInformation(
            "Response: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            correlationId
        );
    }
}