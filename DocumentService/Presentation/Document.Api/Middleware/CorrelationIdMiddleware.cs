using Document.Api.Constants;

namespace Document.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[ApiConstants.Headers.CorrelationId].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items[ApiConstants.Headers.CorrelationId] = correlationId;
        context.Response.Headers.Append(ApiConstants.Headers.CorrelationId, correlationId);

        await _next(context);
    }
}