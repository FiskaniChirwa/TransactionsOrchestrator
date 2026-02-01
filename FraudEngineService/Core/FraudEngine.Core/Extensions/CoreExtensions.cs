using FraudEngine.Core.Rules;
using FraudEngine.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FraudEngine.Core.Extensions;

public static class CoreExtensions
{
    public static IServiceCollection AddFraudEngine(this IServiceCollection services)
    {
        services.AddSingleton(new RuleConfiguration());

        services.AddScoped<IFraudRule, HighAmountRule>();
        services.AddScoped<IFraudRule, UnusualTimeRule>();
        services.AddScoped<IFraudRule, VelocityRule>();
        services.AddScoped<IFraudRule, FirstTimeCategoryRule>();
        services.AddScoped<IFraudRule, HighRiskCategoryRule>();

        services.AddScoped<IRiskCalculator, RiskCalculator>();
        services.AddScoped<IFraudAnalysisService, FraudAnalysisService>();

        return services;
    }
}