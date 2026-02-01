using Asp.Versioning;

namespace Document.Api.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersionings(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("X-API-Version"),
                    new QueryStringApiVersionReader("api-version"));
            });

        return services;
    }
}