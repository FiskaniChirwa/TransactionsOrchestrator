namespace FraudEngine.Core.Models;

public class RuleResult
{
    public string RuleName { get; set; } = string.Empty;
    public bool IsTriggered { get; set; }
    public int RiskScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = [];
}