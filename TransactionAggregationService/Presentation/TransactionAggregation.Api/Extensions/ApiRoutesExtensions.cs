
using Asp.Versioning;
using TransactionAggregation.Api.Routes;

namespace TransactionAggregation.Api.Extensions;

public static class ApiRoutesExtensions
{
    public static IEndpointRouteBuilder MapApiRoutes(this IEndpointRouteBuilder endpoint)
    {
        var versionSet = endpoint.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        endpoint.MapHealthEndpoints();
        endpoint.MapTransactionEndpoints();

        return endpoint;
    }
}