using FraudEngine.Api.Middleware;
using Serilog.Context;

namespace FraudEngine.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IServiceCollection AddCustomMiddleware(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<CorrelationIdMiddleware>();
        builder.UseCorrelationIdLogging();
        builder.UseMiddleware<RequestLoggingMiddleware>();        
        builder.UseExceptionHandler();

        return builder;
    }
    
    private static IApplicationBuilder UseCorrelationIdLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Items[Constants.ApiConstants.Headers.CorrelationId]?.ToString()
                ?? context.TraceIdentifier;
            
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
        
        return app;
    }
}