using FraudEngine.Core.Common;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;
using FraudEngine.Models.Responses;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Services;

public class FraudAnalysisService(
    IEnumerable<IFraudRule> rules,
    IRiskCalculator riskCalculator,
    IFraudAlertRepository alertRepository,
    ITransactionHistoryRepository historyRepository,
    ILogger<FraudAnalysisService> logger) : IFraudAnalysisService
{
    private readonly IEnumerable<IFraudRule> _rules = rules;
    private readonly IRiskCalculator _riskCalculator = riskCalculator;
    private readonly IFraudAlertRepository _alertRepository = alertRepository;
    private readonly ITransactionHistoryRepository _historyRepository = historyRepository;
    private readonly ILogger<FraudAnalysisService> _logger = logger;

    public async Task<Result<FraudAnalysisResponse>> AnalyzeAsync(Transaction transaction)
    {
        try
        {
            _logger.LogInformation(
                "Starting fraud analysis for transaction {TransactionId}, customer {CustomerId}",
                transaction.TransactionId,
                transaction.CustomerId);

            // 1. Evaluate all rules
            var ruleResults = new List<RuleResult>();
            foreach (var rule in _rules)
            {
                try
                {
                    var result = await rule.EvaluateAsync(transaction);
                    ruleResults.Add(result);

                    _logger.LogDebug(
                        "Rule {RuleName} evaluated: Triggered={IsTriggered}, Score={Score}",
                        rule.RuleName,
                        result.IsTriggered,
                        result.RiskScore);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Rule {RuleName} failed for transaction {TransactionId}",
                        rule.RuleName,
                        transaction.TransactionId);
                    
                    continue;
                }
            }

            // 2. Calculate risk score
            var riskScore = _riskCalculator.CalculateRiskScore(ruleResults);
            var status = _riskCalculator.DetermineStatus(riskScore);
            var triggeredRules = ruleResults.Where(r => r.IsTriggered).ToList();
            var reason = _riskCalculator.GenerateReason(triggeredRules);
            var isFraudulent = riskScore >= 40;

            _logger.LogInformation(
                "Fraud analysis completed for transaction {TransactionId}: RiskScore={RiskScore}, Status={Status}, RulesTriggered={RuleCount}",
                transaction.TransactionId,
                riskScore,
                status,
                triggeredRules.Count);

            // 3. Create fraud alert (domain model)
            var alert = new Alert
            {
                TransactionId = transaction.TransactionId,
                CustomerId = transaction.CustomerId,
                AccountId = transaction.AccountId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                MerchantName = transaction.MerchantName,
                Category = transaction.Category,
                RiskScore = riskScore,
                IsFraudulent = isFraudulent,
                Status = status,
                RulesTriggered = triggeredRules.Select(r => r.RuleName).ToList(),
                Reason = reason,
                TransactionDate = transaction.TransactionDate
            };

            await _alertRepository.AddAsync(alert);

            // 4. Store transaction in history
            await _historyRepository.AddAsync(transaction);

            // 5. Build response (shared DTO)
            var response = new FraudAnalysisResponse
            {
                TransactionId = transaction.TransactionId,
                IsFraudulent = isFraudulent,
                RiskScore = riskScore,
                RulesTriggered = triggeredRules.Select(r => r.RuleName).ToList(),
                Status = status,
                AnalyzedAt = DateTime.UtcNow,
                Reason = reason
            };

            return Result<FraudAnalysisResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to analyze transaction {TransactionId}",
                transaction.TransactionId);

            return Result<FraudAnalysisResponse>.Failure(
                "Internal error during fraud analysis",
                "ANALYSIS_ERROR");
        }
    }
}