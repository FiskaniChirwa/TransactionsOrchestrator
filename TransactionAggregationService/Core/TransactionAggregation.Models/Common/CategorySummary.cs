namespace TransactionAggregation.Models.Common;

public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public decimal AverageTransactionAmount { get; set; }
}