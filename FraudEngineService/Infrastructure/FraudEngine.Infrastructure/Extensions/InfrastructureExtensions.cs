using FraudEngine.Core.Repositories;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraudEngine.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FraudEngineDb")
            ?? "Data Source=fraudengine.db";

        services.AddDbContext<FraudEngineDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
        services.AddScoped<IFraudAlertRepository, FraudAlertRepository>();
        services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();

        return services;
    }
}