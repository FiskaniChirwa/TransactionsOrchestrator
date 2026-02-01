using FraudEngine.Core.Models;

namespace FraudEngine.Core.Repositories;

public interface IFraudAlertRepository
{
    Task<Alert> AddAsync(Alert alert);
    Task<Alert?> GetByIdAsync(long id);
    Task<Alert?> GetByTransactionIdAsync(long transactionId);
    Task<List<Alert>> GetByCustomerIdAsync(long customerId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<Alert>> GetAllAsync(int skip = 0, int take = 50);
}