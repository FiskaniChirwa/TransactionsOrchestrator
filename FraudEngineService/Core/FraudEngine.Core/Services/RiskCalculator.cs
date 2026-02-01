using FraudEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Services;

public class RiskCalculator(ILogger<RiskCalculator> logger) : IRiskCalculator
{
    private readonly ILogger<RiskCalculator> _logger = logger;

    public int CalculateRiskScore(List<RuleResult> ruleResults)
    {
        var totalScore = ruleResults
            .Where(r => r.IsTriggered)
            .Sum(r => r.RiskScore);

        var cappedScore = Math.Min(totalScore, 100);

        _logger.LogDebug(
            "Risk score calculated: {Score} (raw: {RawScore}) from {RuleCount} triggered rules",
            cappedScore,
            totalScore,
            ruleResults.Count(r => r.IsTriggered));

        return cappedScore;
    }

    public string DetermineStatus(int riskScore)
    {
        return riskScore switch
        {
            >= 70 => "Blocked",
            >= 40 => "Flagged",
            _ => "Clear"
        };
    }

    public string GenerateReason(List<RuleResult> triggeredRules)
    {
        if (!triggeredRules.Any())
            return "No fraud indicators detected";

        var reasons = triggeredRules
            .Where(r => r.IsTriggered)
            .Select(r => r.Reason)
            .Where(r => !string.IsNullOrEmpty(r));

        return string.Join("; ", reasons);
    }
}