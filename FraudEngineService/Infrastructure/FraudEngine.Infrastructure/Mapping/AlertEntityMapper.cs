using FraudEngine.Core.Models;
using FraudEngine.Infrastructure.Data.Entities;
using System.Text.Json;

namespace FraudEngine.Infrastructure.Mapping;

public static class AlertEntityMapper
{
    public static FraudAlertEntity ToEntity(this Alert alert)
    {
        return new FraudAlertEntity
        {
            Id = alert.Id,
            TransactionId = alert.TransactionId,
            CustomerId = alert.CustomerId,
            AccountId = alert.AccountId,
            Amount = alert.Amount,
            Currency = alert.Currency,
            MerchantName = alert.MerchantName,
            MerchantCategory = alert.Category,
            RiskScore = alert.RiskScore,
            IsFraudulent = alert.IsFraudulent,
            Status = alert.Status,
            RulesTriggered = JsonSerializer.Serialize(alert.RulesTriggered),
            Reason = alert.Reason,
            TransactionDate = alert.TransactionDate,
            CreatedAt = alert.CreatedAt
        };
    }

    public static Alert ToDomain(this FraudAlertEntity entity)
    {
        return new Alert
        {
            Id = entity.Id,
            TransactionId = entity.TransactionId,
            CustomerId = entity.CustomerId,
            AccountId = entity.AccountId,
            Amount = entity.Amount,
            Currency = entity.Currency,
            MerchantName = entity.MerchantName,
            Category = entity.MerchantCategory,
            RiskScore = entity.RiskScore,
            IsFraudulent = entity.IsFraudulent,
            Status = entity.Status,
            RulesTriggered = JsonSerializer.Deserialize<List<string>>(entity.RulesTriggered) ?? new(),
            Reason = entity.Reason,
            TransactionDate = entity.TransactionDate,
            CreatedAt = entity.CreatedAt
        };
    }
}