using FraudEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Rules;

public class HighAmountRule : IFraudRule
{
    private readonly RuleConfiguration _config;
    private readonly ILogger<HighAmountRule> _logger;

    public string RuleName => "HighAmountForCategory";

    public HighAmountRule(RuleConfiguration config, ILogger<HighAmountRule> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<RuleResult> EvaluateAsync(Transaction transaction)
    {
        var threshold = _config.CategoryAmountThresholds.GetValueOrDefault(
            transaction.Category,
            _config.CategoryAmountThresholds["Default"]);

        var isTriggered = transaction.Amount > threshold;

        var result = new RuleResult
        {
            RuleName = RuleName,
            IsTriggered = isTriggered,
            RiskScore = isTriggered ? _config.HighAmountRuleWeight : 0,
            Reason = isTriggered
                ? $"Amount R{transaction.Amount:N2} exceeds threshold R{threshold:N2} for category '{transaction.Category}'"
                : string.Empty,
            Metadata = new Dictionary<string, object>
            {
                { "Amount", transaction.Amount },
                { "Threshold", threshold },
                { "Category", transaction.Category }
            }
        };

        if (isTriggered)
        {
            _logger.LogWarning(
                "High amount detected: Transaction {TransactionId} - {Reason}",
                transaction.TransactionId,
                result.Reason);
        }

        return Task.FromResult(result);
    }
}