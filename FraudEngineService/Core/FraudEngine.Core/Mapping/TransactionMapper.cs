using FraudEngine.Core.Models;
using FraudEngine.Models.Events;

namespace FraudEngine.Core.Mapping;

public static class TransactionMapper
{
    public static Transaction ToDomain(this TransactionEvent eventModel)
    {
        return new Transaction
        {
            TransactionId = eventModel.TransactionId,
            CustomerId = eventModel.CustomerId,
            AccountId = eventModel.AccountId,
            Amount = eventModel.Amount,
            Currency = eventModel.Currency,
            MerchantName = eventModel.MerchantName,
            MerchantCode = eventModel.MerchantCode,
            Category = eventModel.MerchantCategory,
            TransactionDate = eventModel.TransactionDate,
            TransactionType = eventModel.TransactionType
        };
    }
}