using FraudEngine.Core.Models;
using FraudEngine.Models.Responses;

namespace FraudEngine.Core.Mapping;

public static class AlertMapper
{
    public static FraudAlertResponse ToResponse(this Alert alert)
    {
        return new FraudAlertResponse
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
            RulesTriggered = alert.RulesTriggered,
            Reason = alert.Reason,
            TransactionDate = alert.TransactionDate,
            CreatedAt = alert.CreatedAt
        };
    }
}