using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionAggregation.Infrastructure.BackgroundServices;
using TransactionAggregation.Infrastructure.Caching;
using TransactionAggregation.Infrastructure.Clients;
using TransactionAggregation.Infrastructure.Configuration;
using TransactionAggregation.Infrastructure.Data;
using TransactionAggregation.Infrastructure.Outbox;
using TransactionAggregation.Infrastructure.Resilience;
using TransactionAggregation.Infrastructure.Repositories;

namespace TransactionAggregation.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<MockProviderConfiguration>(
            configuration.GetSection("MockProviders"));
        services.Configure<OutboxProcessorConfiguration>(
            configuration.GetSection("OutboxProcessor"));
        services.Configure<FraudEngineConfiguration>(
            configuration.GetSection("FraudEngineApiClient"));
        services.Configure<DocumentConfiguration>(
            configuration.GetSection("DocumentApiClient"));

        // Add Outbox DbContext
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection")));

        // Add Outbox Repository
        services.AddScoped<OutboxRepository>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();

        // Resilience
        services.AddSingleton<ResiliencePolicyFactory>();

        // HTTP Clients
        services.AddHttpClient<ICustomerApiClient, CustomerApiClient>();
        services.AddHttpClient<ITransactionApiClient, TransactionApiClient>();
        services.AddHttpClient<IAccountApiClient, AccountApiClient>();
        services.AddHttpClient<IFraudEngineApiClient, FraudEngineApiClient>();
        services.AddHttpClient<IDocumentApiClient, DocumentApiClient>();

        // Add Background Worker
        services.AddHostedService<OutboxProcessorService>();

        return services;
    }
}