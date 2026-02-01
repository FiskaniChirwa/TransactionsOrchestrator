using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionAggregation.Infrastructure.Outbox.Models;
using TransactionAggregation.Infrastructure.Repositories;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Outbox;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly OutboxRepository _outboxRepository;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        OutboxRepository outboxRepository,
        ILogger<OutboxPublisher> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task PublishTransactionsAsync(
        List<TransactionResponse> transactions,
        long customerId)
    {
        _logger.LogInformation(
            "Publishing {Count} transactions to Fraud Engine outbox for customer {CustomerId}",
            transactions.Count,
            customerId);

        var outboxMessages = new List<OutboxMessage>();

        foreach (var transaction in transactions)
        {
            var transactionEvent = new TransactionEvent
            {
                TransactionId = transaction.TransactionId,
                CustomerId = customerId,
                AccountId = transaction.AccountId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                MerchantName = transaction.MerchantName,
                MerchantCode = transaction.MerchantCode,
                MerchantCategory = transaction.MerchantCategory ?? "Unknown",
                TransactionDate = transaction.Date,
                TransactionType = transaction.Type
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = "TransactionCreated",
                Payload = JsonSerializer.Serialize(transactionEvent),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                AttemptCount = 0
            };

            outboxMessages.Add(outboxMessage);
        }

        await _outboxRepository.AddBatchAsync(outboxMessages);

        _logger.LogInformation(
            "Successfully published {Count} transactions to outbox for customer {CustomerId}",
            outboxMessages.Count,
            customerId);
    }
}