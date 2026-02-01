using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Rules;

public class VelocityRule(
    RuleConfiguration config,
    ITransactionHistoryRepository historyRepository,
    ILogger<VelocityRule> logger) : IFraudRule
{
    private readonly RuleConfiguration _config = config;
    private readonly ITransactionHistoryRepository _historyRepository = historyRepository;
    private readonly ILogger<VelocityRule> _logger = logger;

    public string RuleName => "VelocityCheck";

    public async Task<RuleResult> EvaluateAsync(Transaction transaction)
    {
        var windowStart = transaction.TransactionDate.AddMinutes(-_config.VelocityWindowMinutes);
        
        var count = await _historyRepository.CountRecentTransactionsAsync(
            transaction.CustomerId,
            transaction.Category,
            windowStart);

        var totalCount = count + 1;
        var isTriggered = totalCount >= _config.VelocityThreshold;

        var result = new RuleResult
        {
            RuleName = RuleName,
            IsTriggered = isTriggered,
            RiskScore = isTriggered ? _config.VelocityRuleWeight : 0,
            Reason = isTriggered
                ? $"{totalCount} transactions in '{transaction.Category}' within {_config.VelocityWindowMinutes} minutes"
                : string.Empty,
            Metadata = new Dictionary<string, object>
            {
                { "TransactionCount", totalCount },
                { "WindowMinutes", _config.VelocityWindowMinutes },
                { "Threshold", _config.VelocityThreshold },
                { "Category", transaction.Category }
            }
        };

        if (isTriggered)
        {
            _logger.LogWarning(
                "Velocity threshold exceeded: Transaction {TransactionId} - {Reason}",
                transaction.TransactionId,
                result.Reason);
        }

        return result;
    }
}