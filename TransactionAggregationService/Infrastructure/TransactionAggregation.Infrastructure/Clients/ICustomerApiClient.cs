using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Clients;

public interface ICustomerApiClient
{
    Task<Result<CustomerResponse>> GetCustomerAsync(long customerId);
    Task<Result<List<AccountResponse>>> GetCustomerAccountsAsync(long customerId);
}