using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionAggregation.Core.Services;
using TransactionAggregation.Infrastructure.Clients;
using TransactionAggregation.Infrastructure.Outbox;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;
using Xunit;

namespace TransactionAggregation.Tests.Unit.Core.Services;

public class TransactionAggregationServiceTests
{
    private readonly Mock<ICustomerApiClient> _customerApiClientMock;
    private readonly Mock<ITransactionApiClient> _transactionApiClientMock;
    private readonly Mock<IAccountApiClient> _accountApiClientMock;
    private readonly Mock<ICategorizationService> _categorizationServiceMock;
    private readonly Mock<IDocumentApiClient> _documentApiClientMock;
    private readonly Mock<IOutboxPublisher> _outboxPublisher;
    private readonly Mock<ILogger<TransactionAggregationService>> _loggerMock;
    private readonly TransactionAggregationService _sut;

    public TransactionAggregationServiceTests()
    {
        _customerApiClientMock = new Mock<ICustomerApiClient>();
        _transactionApiClientMock = new Mock<ITransactionApiClient>();
        _accountApiClientMock = new Mock<IAccountApiClient>();
        _documentApiClientMock = new Mock<IDocumentApiClient>();
        _categorizationServiceMock = new Mock<ICategorizationService>();
        _outboxPublisher = new Mock<IOutboxPublisher>();
        _loggerMock = new Mock<ILogger<TransactionAggregationService>>();

        _sut = new TransactionAggregationService(
            _customerApiClientMock.Object,
            _transactionApiClientMock.Object,
            _accountApiClientMock.Object,
            _documentApiClientMock.Object,
            _categorizationServiceMock.Object,
            _outboxPublisher.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetCustomerTransactionsAsync_WithValidCustomer_ReturnsAggregatedData()
    {
        // Arrange
        var customerId = 1001L;
        var customer = new CustomerResponse { CustomerId = customerId, FirstName = "John", LastName = "Smith" };
        var accounts = new List<AccountResponse>
        {
            new() { AccountId = 10000, AccountType = "Checking" }
        };
        var transactions = new List<TransactionResponse>
        {
            new() { TransactionId = 1, Amount = -100, MerchantName = "WOOLWORTHS" }
        };
        var balance = new AccountBalanceResponse { AccountId = 10000, CurrentBalance = 5000 };

        _customerApiClientMock
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(Result<CustomerResponse>.SuccessResult(customer));

        _customerApiClientMock
            .Setup(x => x.GetCustomerAccountsAsync(customerId))
            .ReturnsAsync(Result<List<AccountResponse>>.SuccessResult(accounts));

        _transactionApiClientMock
            .Setup(x => x.GetTransactionsAsync(10000, null, null, 1, 50))
            .ReturnsAsync(Result<TransactionListResponse>.SuccessResult(new TransactionListResponse
            {
                Transactions = transactions,
                PageInfo = new PageInfo()
            }));

        _accountApiClientMock
            .Setup(x => x.GetAccountBalanceAsync(10000))
            .ReturnsAsync(Result<AccountBalanceResponse>.SuccessResult(balance));

        _categorizationServiceMock
            .Setup(x => x.CategorizeTransaction(It.IsAny<TransactionResponse>()))
            .Returns("Groceries");

        // Act
        var result = await _sut.GetCustomerTransactionsAsync(customerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CustomerId.Should().Be(customerId);
        result.Data.CustomerName.Should().Be("John Smith");
        result.Data.Accounts.Should().HaveCount(1);
        result.Data.TotalTransactions.Should().Be(1);
    }

    [Fact]
    public async Task GetCustomerTransactionsAsync_WhenCustomerNotFound_ReturnsFailure()
    {
        // Arrange
        var customerId = 9999L;
        _customerApiClientMock
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(Result<CustomerResponse>.FailureResult("Customer not found", "NOT_FOUND"));

        // Act
        var result = await _sut.GetCustomerTransactionsAsync(customerId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
        result.ErrorMessage.Should().Be("Customer not found");
    }

    [Fact]
    public async Task GetCustomerTransactionsAsync_CategorizesAllTransactions()
    {
        // Arrange
        var customerId = 1001L;
        SetupValidCustomerWithAccounts(customerId);

        var transactions = new List<TransactionResponse>
        {
            new() { TransactionId = 1, MerchantName = "WOOLWORTHS" },
            new() { TransactionId = 2, MerchantName = "UBER" },
            new() { TransactionId = 3, MerchantName = "NETFLIX" }
        };

        _transactionApiClientMock
            .Setup(x => x.GetTransactionsAsync(It.IsAny<long>(), null, null, 1, 50))
            .ReturnsAsync(Result<TransactionListResponse>.SuccessResult(new TransactionListResponse
            {
                Transactions = transactions,
                PageInfo = new PageInfo()
            }));

        _categorizationServiceMock
            .Setup(x => x.CategorizeTransaction(It.IsAny<TransactionResponse>()))
            .Returns("TestCategory");

        // Act
        var result = await _sut.GetCustomerTransactionsAsync(customerId);

        // Assert
        _categorizationServiceMock.Verify(
            x => x.CategorizeTransaction(It.IsAny<TransactionResponse>()),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task GetCustomerTransactionsAsync_WithDateFilter_PassesToTransactionClient()
    {
        // Arrange
        var customerId = 1001L;
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 31);

        SetupValidCustomerWithAccounts(customerId);

        // Act
        await _sut.GetCustomerTransactionsAsync(customerId, fromDate, toDate);

        // Assert
        _transactionApiClientMock.Verify(
            x => x.GetTransactionsAsync(It.IsAny<long>(), fromDate, toDate, 1, 50),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCustomerTransactionsAsync_WhenAccountFailsToLoad_ContinuesWithOthers()
    {
        // Arrange
        var customerId = 1001L;
        var customer = new CustomerResponse { CustomerId = customerId, FirstName = "John", LastName = "Smith" };
        var accounts = new List<AccountResponse>
        {
            new() { AccountId = 10000, AccountType = "Checking" },
            new() { AccountId = 10001, AccountType = "Savings" }
        };

        _customerApiClientMock
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(Result<CustomerResponse>.SuccessResult(customer));

        _customerApiClientMock
            .Setup(x => x.GetCustomerAccountsAsync(customerId))
            .ReturnsAsync(Result<List<AccountResponse>>.SuccessResult(accounts));

        // First account succeeds
        _transactionApiClientMock
            .Setup(x => x.GetTransactionsAsync(10000, null, null, 1, 50))
            .ReturnsAsync(Result<TransactionListResponse>.SuccessResult(new TransactionListResponse
            {
                Transactions = new List<TransactionResponse> { new() { TransactionId = 1 } },
                PageInfo = new PageInfo()
            }));

        // Second account fails
        _transactionApiClientMock
            .Setup(x => x.GetTransactionsAsync(10001, null, null, 1, 50))
            .ReturnsAsync(Result<TransactionListResponse>.FailureResult("Failed", "ERROR"));

        _accountApiClientMock
            .Setup(x => x.GetAccountBalanceAsync(It.IsAny<long>()))
            .ReturnsAsync(Result<AccountBalanceResponse>.SuccessResult(new AccountBalanceResponse()));

        _categorizationServiceMock
            .Setup(x => x.CategorizeTransaction(It.IsAny<TransactionResponse>()))
            .Returns("Test");

        // Act
        var result = await _sut.GetCustomerTransactionsAsync(customerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Accounts.Should().HaveCount(1); // Only successful account
        result.Data.TotalTransactions.Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionSummaryAsync_CalculatesTotalsCorrectly()
    {
        // Arrange
        var customerId = 1001L;
        SetupValidCustomerWithAccounts(customerId);

        var transactions = new List<TransactionResponse>
        {
            new() { TransactionId = 1, Amount = -100, Type ="Debit" },
            new() { TransactionId = 2, Amount = -200, Type = "Debit" },
            new() { TransactionId = 3, Amount = 5000, Type = "Credit" }
        };

        _transactionApiClientMock
            .Setup(x => x.GetTransactionsAsync(It.IsAny<long>(), null, null, 1, 50))
            .ReturnsAsync(Result<TransactionListResponse>.SuccessResult(new TransactionListResponse
            {
                Transactions = transactions,
                PageInfo = new PageInfo()
            }));

        _categorizationServiceMock
            .Setup(x => x.CategorizeTransaction(It.IsAny<TransactionResponse>()))
            .Returns("Test");

        _categorizationServiceMock
            .Setup(x => x.GroupTransactionsByCategory(It.IsAny<List<TransactionResponse>>()))
            .Returns(new List<CategorySummary>());

        // Act
        var result = await _sut.GetTransactionSummaryAsync(customerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TotalDebits.Should().Be(-300);
        result.Data.TotalCredits.Should().Be(5000);
        result.Data.NetAmount.Should().Be(4700);
    }

    // Helper methods
    private void SetupValidCustomerWithAccounts(long customerId)
    {
        var customer = new CustomerResponse { CustomerId = customerId, FirstName = "Test", LastName = "User" };
        var accounts = new List<AccountResponse>
        {
            new() { AccountId = 10000, AccountType = "Checking" }
        };

        _customerApiClientMock
            .Setup(x => x.GetCustomerAsync(customerId))
            .ReturnsAsync(Result<CustomerResponse>.SuccessResult(customer));

        _customerApiClientMock
            .Setup(x => x.GetCustomerAccountsAsync(customerId))
            .ReturnsAsync(Result<List<AccountResponse>>.SuccessResult(accounts));

        _accountApiClientMock
            .Setup(x => x.GetAccountBalanceAsync(It.IsAny<long>()))
            .ReturnsAsync(Result<AccountBalanceResponse>.SuccessResult(new AccountBalanceResponse()));
    }
}