using System.Diagnostics;
using Document.Api.Constants;
using Document.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Document.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[ApiConstants.Headers.CorrelationId]?.ToString()
            ?? httpContext.TraceIdentifier;
        
        var (statusCode, errorCode, message) = MapException(exception);

        // Log with appropriate level and correlation ID
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "[{CorrelationId}] Server error - {ErrorCode}: {Message}",
                correlationId,
                errorCode,
                exception.Message);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "[{CorrelationId}] Client error - {ErrorCode}: {Message}",
                correlationId,
                errorCode,
                exception.Message);
        }

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = GetTitle(statusCode),
            status = statusCode,
            detail = message,
            errorCode = errorCode,
            correlationId = correlationId,
            traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private (int StatusCode, string ErrorCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            DocumentValidationException validationEx => (
                400,
                "VALIDATION_ERROR",
                validationEx.Message
            ),

            TemplateNotFoundException templateEx => (
                400,
                "TEMPLATE_NOT_FOUND",
                $"Document type '{templateEx.DocumentType}' is not supported"
            ),

            FileNotFoundException or KeyNotFoundException => (
                404,
                "NOT_FOUND",
                "The requested resource was not found"
            ),

            _ => (
                500,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later."
            )
        };
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        404 => "Not Found",
        500 => "Internal Server Error",
        _ => "Error"
    };
}