namespace FraudEngine.Models.Responses;

public class FraudAnalysisResponse
{
    public long TransactionId { get; set; }
    public bool IsFraudulent { get; set; }
    public int RiskScore { get; set; }
    public List<string> RulesTriggered { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}