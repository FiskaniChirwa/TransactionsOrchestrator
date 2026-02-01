using FraudEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Rules;

public class HighRiskCategoryRule(RuleConfiguration config, ILogger<HighRiskCategoryRule> logger) : IFraudRule
{
    private readonly RuleConfiguration _config = config;
    private readonly ILogger<HighRiskCategoryRule> _logger = logger;

    public string RuleName => "HighRiskCategory";

    public Task<RuleResult> EvaluateAsync(Transaction transaction)
    {
        var isHighRisk = _config.HighRiskCategories.Contains(transaction.Category);

        var result = new RuleResult
        {
            RuleName = RuleName,
            IsTriggered = isHighRisk,
            RiskScore = isHighRisk ? _config.HighRiskCategoryWeight : 0,
            Reason = isHighRisk
                ? $"Transaction in high-risk category '{transaction.Category}'"
                : string.Empty,
            Metadata = new Dictionary<string, object>
            {
                { "Category", transaction.Category },
                { "IsHighRisk", isHighRisk }
            }
        };

        if (isHighRisk)
        {
            _logger.LogWarning(
                "High-risk category detected: Transaction {TransactionId} - {Category}",
                transaction.TransactionId,
                transaction.Category);
        }

        return Task.FromResult(result);
    }
}