using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Models.Responses;

public class TransactionSummaryResponse
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateRangeFilter? DateRange { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal NetAmount { get; set; }
    public List<CategorySummary> CategorySummaries { get; set; } = [];
    public List<AccountResponse> Accounts { get; set; } = [];
}