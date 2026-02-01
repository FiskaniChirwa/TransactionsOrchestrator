using FraudEngine.Core.Models;

namespace FraudEngine.Core.Rules;

public interface IFraudRule
{
    string RuleName { get; }
    Task<RuleResult> EvaluateAsync(Transaction transaction);
}