using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Outbox;

public interface IOutboxPublisher
{
    Task PublishTransactionsAsync(List<TransactionResponse> transactions, long customerId);
}