using Asp.Versioning;

namespace TransactionAggregation.Api.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersionings(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(majorVersion: 1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        });

        return services;
    }
}