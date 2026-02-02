namespace TransactionAggregation.Api.Extensions;

using TransactionAggregation.Api.Middleware;


public static class MiddlewareExtensions
{
    public static IServiceCollection AddCustomMiddleware(this IServiceCollection services)
    {
        // TODO: Fix    
        // services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
        });
        return services;
    }

    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<CorrelationIdMiddleware>();
        builder.UseMiddleware<RequestLoggingMiddleware>();
        builder.UseExceptionHandler();

        return builder;
    }
}