using Microsoft.Extensions.DependencyInjection;
using TransactionAggregation.Core.Services;

namespace TransactionAggregation.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionAggregationService, TransactionAggregationService>();
        services.AddScoped<ICategorizationService, CategorizationService>();


        return services;
    }
}