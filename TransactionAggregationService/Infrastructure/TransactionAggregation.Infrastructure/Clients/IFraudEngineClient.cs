using TransactionAggregation.Models.Common;

namespace TransactionAggregation.Infrastructure.Clients;

public interface IFraudEngineApiClient
{
    Task<Result> SendEventAsync(string eventId, string payload);
}