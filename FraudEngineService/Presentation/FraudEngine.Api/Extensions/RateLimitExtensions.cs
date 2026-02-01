using System.Threading.RateLimiting;
using System.Diagnostics;
using FraudEngine.Api.Constants;

namespace FraudEngine.Api.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        const string PolicyName = "FraudEnginePolicy";

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, httpContext =>
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(remoteIp, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 3,
                    QueueLimit = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var correlationId = context.HttpContext.Items[ApiConstants.Headers.CorrelationId]?.ToString() 
                                    ?? context.HttpContext.TraceIdentifier;

                var retryAfter = "60"; 
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = ((int)retryAfterValue.TotalSeconds).ToString();
                }
                context.HttpContext.Response.Headers.RetryAfter = retryAfter;

                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "You are generating documents too quickly. Please wait before trying again.",
                    errorCode = "RATE_LIMIT_EXCEEDED",
                    correlationId = correlationId,
                    traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, token);
            };
        });

        return services;
    }
}