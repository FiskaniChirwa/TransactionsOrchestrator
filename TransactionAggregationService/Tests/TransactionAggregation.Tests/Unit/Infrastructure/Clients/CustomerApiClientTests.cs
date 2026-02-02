// using FluentAssertions;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Moq;
// using RichardSzalay.MockHttp;
// using System.Net;
// using TransactionAggregation.Infrastructure.Caching;
// using TransactionAggregation.Infrastructure.Clients;
// using TransactionAggregation.Infrastructure.Configuration;
// using TransactionAggregation.Infrastructure.Resilience;
// using TransactionAggregation.Models.Contracts;
// using Xunit;

// namespace TransactionAggregation.UnitTests.Infrastructure.Clients;

// public class CustomerApiClientTests
// {
//     private readonly MockHttpMessageHandler _mockHttp;
//     private readonly Mock<ICacheService> _cacheServiceMock;
//     private readonly Mock<ILogger<CustomerApiClient>> _loggerMock;
//     private readonly CustomerApiClient _sut;
//     private readonly MockProviderConfiguration _config;

//     public CustomerApiClientTests()
//     {
//         _mockHttp = new MockHttpMessageHandler();
//         _cacheServiceMock = new Mock<ICacheService>();
//         _loggerMock = new Mock<ILogger<CustomerApiClient>>();
        
//         _config = new MockProviderConfiguration
//         {
//             CustomerServiceUrl = "http://localhost:5001",
//             TimeoutSeconds = 5,
//             RetryCount = 2
//         };

//         var httpClient = _mockHttp.ToHttpClient();
//         var options = Options.Create(_config);
//         var policyFactory = new ResiliencePolicyFactory(_config);

//         _sut = new CustomerApiClient(
//             httpClient,
//             _loggerMock.Object,
//             _cacheServiceMock.Object,
//             options,
//             policyFactory
//         );
//     }

//     [Fact]
//     public async Task GetCustomerAsync_WithValidId_ReturnsCustomer()
//     {
//         // Arrange
//         var customerId = 1001L;
//         var expectedCustomer = new CustomerResponse
//         {
//             CustomerId = customerId,
//             FirstName = "John",
//             LastName = "Smith",
//             Email = "john.smith@email.com"
//         };

//         _mockHttp
//             .When($"{_config.CustomerServiceUrl}/api/customers/{customerId}")
//             .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(expectedCustomer));

//         _cacheServiceMock
//             .Setup(x => x.GetOrCreateAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<Func<Task<CustomerResponse>>>(),
//                 It.IsAny<TimeSpan>()))
//             .Returns<string, Func<Task<CustomerResponse>>, TimeSpan>(
//                 async (key, factory, ttl) => await factory());

//         // Act
//         var result = await _sut.GetCustomerAsync(customerId);

//         // Assert
//         result.Success.Should().BeTrue();
//         result.Data.Should().NotBeNull();
//         result.Data!.CustomerId.Should().Be(customerId);
//         result.Data.FirstName.Should().Be("John");
//     }

//     [Fact]
//     public async Task GetCustomerAsync_WhenNotFound_ReturnsFailure()
//     {
//         // Arrange
//         var customerId = 9999L;

//         _mockHttp
//             .When($"{_config.CustomerServiceUrl}/api/customers/{customerId}")
//             .Respond(HttpStatusCode.NotFound);

//         _cacheServiceMock
//             .Setup(x => x.GetOrCreateAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<Func<Task<CustomerResponse>>>(),
//                 It.IsAny<TimeSpan>()))
//             .Returns<string, Func<Task<CustomerResponse>>, TimeSpan>(
//                 async (key, factory, ttl) => await factory());

//         // Act
//         var result = await _sut.GetCustomerAsync(customerId);

//         // Assert
//         result.Success.Should().BeFalse();
//         result.ErrorCode.Should().Contain("NOT_FOUND");
//     }

//     [Fact]
//     public async Task GetCustomerAsync_UsesCaching()
//     {
//         // Arrange
//         var customerId = 1001L;
//         var cachedCustomer = new CustomerResponse { CustomerId = customerId };

//         _cacheServiceMock
//             .Setup(x => x.GetOrCreateAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<Func<Task<CustomerResponse>>>(),
//                 It.IsAny<TimeSpan>()))
//             .ReturnsAsync(cachedCustomer);

//         // Act
//         var result = await _sut.GetCustomerAsync(customerId);

//         // Assert
//         result.Success.Should().BeTrue();
//         _cacheServiceMock.Verify(
//             x => x.GetOrCreateAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<Func<Task<CustomerResponse>>>(),
//                 It.IsAny<TimeSpan>()),
//             Times.Once
//         );
//     }

//     [Fact]
//     public async Task GetCustomerAccountsAsync_ReturnsAccounts()
//     {
//         // Arrange
//         var customerId = 1001L;
//         var expectedAccounts = new CustomerAccountsResponse
//         {
//             CustomerId = customerId,
//             Accounts = new List<AccountResponse>
//             {
//                 new() { AccountId = 10000, AccountType = "Checking" }
//             }
//         };

//         _mockHttp
//             .When($"{_config.CustomerServiceUrl}/api/customers/{customerId}/accounts")
//             .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(expectedAccounts));

//         _cacheServiceMock
//             .Setup(x => x.GetOrCreateAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<Func<Task<List<AccountResponse>>>>(),
//                 It.IsAny<TimeSpan>()))
//             .Returns<string, Func<Task<List<AccountResponse>>>, TimeSpan>(
//                 async (key, factory, ttl) => await factory());

//         // Act
//         var result = await _sut.GetCustomerAccountsAsync(customerId);

//         // Assert
//         result.Success.Should().BeTrue();
//         result.Data.Should().HaveCount(1);
//         result.Data![0].AccountId.Should().Be(10000);
//     }
// }