using FraudEngine.Models.Responses;

namespace FraudEngine.Core.Repositories;

public interface IProcessedEventRepository
{
    Task<FraudAnalysisResponse?> GetByEventIdAsync(string eventId);
    Task AddAsync(string eventId, long transactionId, FraudAnalysisResponse result);
    Task<bool> ExistsAsync(string eventId);
}