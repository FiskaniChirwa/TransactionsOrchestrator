using FraudEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Rules;

public class UnusualTimeRule(RuleConfiguration config, ILogger<UnusualTimeRule> logger) : IFraudRule
{
    private readonly RuleConfiguration _config = config;
    private readonly ILogger<UnusualTimeRule> _logger = logger;

    public string RuleName => "UnusualTimeForCategory";

    public Task<RuleResult> EvaluateAsync(Transaction transaction)
    {
        var hour = transaction.TransactionDate.Hour;
        
        var isUnusualTime = hour >= _config.UnusualHourStart || hour < _config.UnusualHourEnd;

        var suspiciousCategories = new[] { "Groceries", "Shopping", "Electronics" };
        var isTriggered = isUnusualTime && suspiciousCategories.Contains(transaction.Category);

        var result = new RuleResult
        {
            RuleName = RuleName,
            IsTriggered = isTriggered,
            RiskScore = isTriggered ? _config.UnusualTimeRuleWeight : 0,
            Reason = isTriggered
                ? $"Transaction in '{transaction.Category}' at unusual time: {transaction.TransactionDate:HH:mm}"
                : string.Empty,
            Metadata = new Dictionary<string, object>
            {
                { "Hour", hour },
                { "Category", transaction.Category },
                { "TransactionTime", transaction.TransactionDate }
            }
        };

        if (isTriggered)
        {
            _logger.LogWarning(
                "Unusual time detected: Transaction {TransactionId} - {Reason}",
                transaction.TransactionId,
                result.Reason);
        }

        return Task.FromResult(result);
    }
}