using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TransactionAggregation.Infrastructure.Clients;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove real API clients
            services.RemoveAll<ICustomerApiClient>();
            services.RemoveAll<ITransactionApiClient>();
            services.RemoveAll<IAccountApiClient>();
            services.RemoveAll<IFraudEngineApiClient>();

            // Add mocked clients with test data
            var mockCustomerClient = CreateMockCustomerClient();
            var mockTransactionClient = CreateMockTransactionClient();
            var mockAccountClient = CreateMockAccountClient();
            var mockFraudClient = CreateMockFraudClient();

            services.AddSingleton(mockCustomerClient.Object);
            services.AddSingleton(mockTransactionClient.Object);
            services.AddSingleton(mockAccountClient.Object);
            services.AddSingleton(mockFraudClient.Object);
        });
    }

    private Mock<ICustomerApiClient> CreateMockCustomerClient()
    {
        var mock = new Mock<ICustomerApiClient>();

        // Setup GetCustomerAsync
        mock.Setup(x => x.GetCustomerAsync(1001))
            .ReturnsAsync(Result<CustomerResponse>.SuccessResult(new CustomerResponse
            {
                CustomerId = 1001,
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@test.com",
                PhoneNumber = "+27 82 123 4567",
                DateOfBirth = new DateOnly(1985, 5, 15),
            }));

        mock.Setup(x => x.GetCustomerAsync(9999))
            .ReturnsAsync(Result<CustomerResponse>.FailureResult(
                "Customer not found", 
                "CUSTOMER_NOT_FOUND"
            ));

        // Setup GetCustomerAccountsAsync
        mock.Setup(x => x.GetCustomerAccountsAsync(1001))
            .ReturnsAsync(Result<List<AccountResponse>>.SuccessResult(new List<AccountResponse>
            {
                new AccountResponse
                {
                    AccountId = 10000,
                    AccountNumber = "ACC-1001-0001",
                    AccountType = "Checking",
                    Currency = "ZAR",
                    Status = "Active",
                    OpenedDate = new DateTime(2020, 1, 15)
                }
            }));

        return mock;
    }

    private Mock<ITransactionApiClient> CreateMockTransactionClient()
    {
        var mock = new Mock<ITransactionApiClient>();

        mock.Setup(x => x.GetTransactionsAsync(
                It.IsAny<long>(), 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>(), 
                It.IsAny<int>(), 
                It.IsAny<int>()))
            .ReturnsAsync((long accountId, DateTime? from, DateTime? to, int page, int pageSize) =>
            {
                var transactions = new List<TransactionResponse>
                {
                    new TransactionResponse
                    {
                        TransactionId = 100001,
                        AccountId = accountId,
                        Date = DateTime.UtcNow.AddDays(-5),
                        Amount = -350.00m,
                        Currency = "ZAR",
                        Description = "POS Purchase - WOOLWORTHS",
                        Type = "Debit",
                        Status = "Completed",
                        MerchantName = "WOOLWORTHS CAPE TOWN",
                        MerchantCode = "5411",
                        MerchantCategory = null,
                        BalanceAfter = 12450.50m
                    },
                    new TransactionResponse
                    {
                        TransactionId = 100002,
                        AccountId = accountId,
                        Date = DateTime.UtcNow.AddDays(-3),
                        Amount = -120.00m,
                        Currency = "ZAR",
                        Description = "POS Purchase - UBER",
                        Type = "Debit",
                        Status = "Completed",
                        MerchantName = "UBER",
                        MerchantCode = "4121",
                        MerchantCategory = null,
                        BalanceAfter = 12330.50m
                    },
                    new TransactionResponse
                    {
                        TransactionId = 100003,
                        AccountId = accountId,
                        Date = DateTime.UtcNow.AddDays(-1),
                        Amount = 5000.00m,
                        Currency = "ZAR",
                        Description = "SALARY DEPOSIT",
                        Type = "Credit",
                        Status = "Completed",
                        MerchantName = null,
                        MerchantCode = null,
                        MerchantCategory = null,
                        BalanceAfter = 17330.50m
                    }
                };

                return Result<TransactionListResponse>.SuccessResult(new TransactionListResponse
                {
                    AccountId = accountId,
                    Transactions = transactions,
                    PageInfo = new PageInfo
                    {
                        TotalCount = transactions.Count,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            });

        return mock;
    }

    private Mock<IAccountApiClient> CreateMockAccountClient()
    {
        var mock = new Mock<IAccountApiClient>();

        mock.Setup(x => x.GetAccountBalanceAsync(It.IsAny<long>()))
            .ReturnsAsync((long accountId) =>
            {
                return Result<AccountBalanceResponse>.SuccessResult(new AccountBalanceResponse
                {
                    AccountId = accountId,
                    CurrentBalance = 17330.50m,
                    AvailableBalance = 17330.50m,
                    Currency = "ZAR",
                    LastUpdated = DateTime.UtcNow
                });
            });

        return mock;
    }

    private Mock<IFraudEngineApiClient> CreateMockFraudClient()
    {
        var mock = new Mock<IFraudEngineApiClient>();

        mock.Setup(x => x.SendEventAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string eventId, string payload) =>
            {
                return Result.SuccessResult();
            });

        return mock;
    }
}