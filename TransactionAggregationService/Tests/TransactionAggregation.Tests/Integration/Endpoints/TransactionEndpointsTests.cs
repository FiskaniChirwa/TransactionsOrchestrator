using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TransactionAggregation.Models.Responses;
using TransactionAggregation.Tests.Helpers;
using Xunit;

namespace TransactionAggregation.IntegrationTests.Endpoints;

public class TransactionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCustomerTransactions_WithValidId_Returns200()
    {
        // Arrange
        var customerId = 1001;

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<AggregatedTransactionResponse>();
        content.Should().NotBeNull();
        content!.CustomerId.Should().Be(customerId);
        content.CustomerName.Should().Be("John Smith");
        content.Accounts.Should().HaveCount(1);
        content.TotalTransactions.Should().Be(3);
    }

    [Fact]
    public async Task GetCustomerTransactions_WithInvalidId_Returns404()
    {
        // Arrange
        var customerId = 9999;

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerTransactions_VerifiesCategorization()
    {
        // Arrange
        var customerId = 1001;

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}/transactions");
        var content = await response.Content.ReadFromJsonAsync<AggregatedTransactionResponse>();

        // Assert
        content!.Accounts[0].Transactions.Should().Contain(t =>
            t.MerchantName == "WOOLWORTHS CAPE TOWN" &&
            t.MerchantCategory == "Groceries"
        );

        content.Accounts[0].Transactions.Should().Contain(t =>
            t.MerchantName == "UBER" &&
            t.MerchantCategory == "Transport"
        );

        content.Accounts[0].Transactions.Should().Contain(t =>
            t.Description == "SALARY DEPOSIT" &&
            t.MerchantCategory == "Income"
        );
    }

    [Fact]
    public async Task GetTransactionSummary_WithValidId_Returns200()
    {
        // Arrange
        var customerId = 1001;

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}/transactions/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<TransactionSummaryResponse>();
        content.Should().NotBeNull();
        content!.CustomerId.Should().Be(customerId);
        content.CategorySummaries.Should().NotBeEmpty();
        content.TotalDebits.Should().Be(-470.00m);
        content.TotalCredits.Should().Be(5000.00m);
        content.NetAmount.Should().Be(4530.00m);
    }

    [Fact]
    public async Task GetTransactionSummary_VerifiesCategoryBreakdown()
    {
        // Arrange
        var customerId = 1001;

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}/transactions/summary");
        var content = await response.Content.ReadFromJsonAsync<TransactionSummaryResponse>();

        // Assert
        var groceries = content!.CategorySummaries.FirstOrDefault(c => c.Category == "Groceries");
        groceries.Should().NotBeNull();
        groceries!.TotalAmount.Should().Be(-350.00m);
        groceries.TransactionCount.Should().Be(1);

        var transport = content.CategorySummaries.FirstOrDefault(c => c.Category == "Transport");
        transport.Should().NotBeNull();
        transport!.TotalAmount.Should().Be(-120.00m);
        transport.TransactionCount.Should().Be(1);

        var income = content.CategorySummaries.FirstOrDefault(c => c.Category == "Income");
        income.Should().NotBeNull();
        income!.TotalAmount.Should().Be(5000.00m);
        income.TransactionCount.Should().Be(1);
    }
}