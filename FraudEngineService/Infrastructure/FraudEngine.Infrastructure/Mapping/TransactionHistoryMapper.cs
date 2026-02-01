using FraudEngine.Core.Models;
using FraudEngine.Infrastructure.Data.Entities;

namespace FraudEngine.Infrastructure.Mapping;

public static class TransactionHistoryMapper
{
    public static TransactionHistoryEntity ToEntity(this Transaction transaction)
    {
        return new TransactionHistoryEntity
        {
            TransactionId = transaction.TransactionId,
            CustomerId = transaction.CustomerId,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            MerchantCategory = transaction.Category,
            TransactionDate = transaction.TransactionDate,
            RecordedAt = DateTime.UtcNow
        };
    }

    public static Transaction ToDomain(this TransactionHistoryEntity entity)
    {
        return new Transaction
        {
            TransactionId = entity.TransactionId,
            CustomerId = entity.CustomerId,
            AccountId = entity.AccountId,
            Amount = entity.Amount,
            Category = entity.MerchantCategory,
            TransactionDate = entity.TransactionDate
        };
    }
}