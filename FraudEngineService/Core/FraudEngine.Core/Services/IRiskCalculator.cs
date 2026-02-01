using FraudEngine.Core.Models;

namespace FraudEngine.Core.Services;

public interface IRiskCalculator
{
    int CalculateRiskScore(List<RuleResult> ruleResults);
    string DetermineStatus(int riskScore);
    string GenerateReason(List<RuleResult> triggeredRules);
}