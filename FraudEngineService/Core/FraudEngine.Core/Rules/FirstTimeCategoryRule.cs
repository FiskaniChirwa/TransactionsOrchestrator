using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Rules;

public class FirstTimeCategoryRule(
    RuleConfiguration config,
    ITransactionHistoryRepository historyRepository,
    ILogger<FirstTimeCategoryRule> logger) : IFraudRule
{
    private readonly RuleConfiguration _config = config;
    private readonly ITransactionHistoryRepository _historyRepository = historyRepository;
    private readonly ILogger<FirstTimeCategoryRule> _logger = logger;

    public string RuleName => "FirstTimeCategory";

    public async Task<RuleResult> EvaluateAsync(Transaction transaction)
    {
        var isFirstTime = await _historyRepository.IsFirstTimeCategoryAsync(
            transaction.CustomerId,
            transaction.Category);

        var result = new RuleResult
        {
            RuleName = RuleName,
            IsTriggered = isFirstTime,
            RiskScore = isFirstTime ? _config.FirstTimeCategoryWeight : 0,
            Reason = isFirstTime
                ? $"First transaction in category '{transaction.Category}'"
                : string.Empty,
            Metadata = new Dictionary<string, object>
            {
                { "Category", transaction.Category },
                { "IsFirstTime", isFirstTime }
            }
        };

        if (isFirstTime)
        {
            _logger.LogInformation(
                "First time category: Transaction {TransactionId} - {Category}",
                transaction.TransactionId,
                transaction.Category);
        }

        return result;
    }
}