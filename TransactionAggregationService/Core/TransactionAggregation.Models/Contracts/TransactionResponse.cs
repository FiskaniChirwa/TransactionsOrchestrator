    namespace TransactionAggregation.Models.Contracts;

    public class TransactionResponse
    {
        public long TransactionId { get; set; }
        public long AccountId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantCode { get; set; } = string.Empty;
        public string? MerchantCategory { get; set; }
        public decimal BalanceAfter { get; set; }
    }