    using TransactionAggregation.Models.Common;

    namespace TransactionAggregation.Models.Responses;

    public class AggregatedTransactionResponse
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<AccountWithTransactions> Accounts { get; set; } = [];
        public int TotalTransactions { get; set; }
        public DateRangeFilter? DateRange { get; set; }
    }