using FluentAssertions;
using TransactionAggregation.Core.Services;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Tests.Unit.Core.Services;

public class CategorizationServiceTests
{
    private readonly CategorizationService _sut;

    public CategorizationServiceTests()
    {
        _sut = new CategorizationService();
    }

    [Theory]
    [InlineData("5411", "Groceries")]
    [InlineData("5812", "Restaurants")]
    [InlineData("5541", "Transport")]
    [InlineData("4900", "Utilities")]
    [InlineData("7995", "Entertainment")]
    [InlineData("5999", "Shopping")]
    [InlineData("9999", "Other")]
    public void CategorizeTransaction_WithMccCode_ReturnsCorrectCategory(string mccCode, string expectedCategory)
    {
        // Arrange
        var transaction = new TransactionResponse
        {
            TransactionId = 1,
            Amount = -100,
            MerchantCode = mccCode,
            MerchantName = "Test Merchant"
        };

        // Act
        var result = _sut.CategorizeTransaction(transaction);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData("WOOLWORTHS CAPE TOWN", "Groceries")]
    [InlineData("CHECKERS SOMERSET", "Groceries")]
    [InlineData("UBER TRIP", "Transport")]
    [InlineData("BOLT RIDE", "Transport")]
    [InlineData("NETFLIX.COM", "Entertainment")]
    [InlineData("MCDONALDS", "Restaurants")]
    [InlineData("CITY OF CAPE TOWN", "Utilities")]
    [InlineData("RANDOM STORE", "Other")]
    public void CategorizeTransaction_WithMerchantName_ReturnsCorrectCategory(string merchantName, string expectedCategory)
    {
        // Arrange
        var transaction = new TransactionResponse
        {
            TransactionId = 1,
            Amount = -100,
            MerchantName = merchantName
        };

        // Act
        var result = _sut.CategorizeTransaction(transaction);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Fact]
    public void CategorizeTransaction_WithPositiveAmount_ReturnsIncome()
    {
        // Arrange
        var transaction = new TransactionResponse
        {
            TransactionId = 1,
            Amount = 5000,
            Description = "SALARY DEPOSIT"
        };

        // Act
        var result = _sut.CategorizeTransaction(transaction);

        // Assert
        result.Should().Be("Income");
    }

    [Fact]
    public void CategorizeTransaction_WithRefundDescription_ReturnsRefund()
    {
        // Arrange
        var transaction = new TransactionResponse
        {
            TransactionId = 1,
            Amount = 350,
            Description = "REFUND - WOOLWORTHS"
        };

        // Act
        var result = _sut.CategorizeTransaction(transaction);

        // Assert
        result.Should().Be("Refund");
    }

    [Fact]
    public void GroupTransactionsByCategory_WithMultipleTransactions_ReturnsCorrectSummaries()
    {
        // Arrange
        var transactions = new List<TransactionResponse>
        {
            new() { TransactionId = 1, Amount = -100, MerchantCode = "5411", MerchantName = "WOOLWORTHS" },
            new() { TransactionId = 2, Amount = -200, MerchantCode = "5411", MerchantName = "CHECKERS" },
            new() { TransactionId = 3, Amount = -150, MerchantCode = "5812", MerchantName = "RESTAURANT" },
            new() { TransactionId = 4, Amount = 5000, Description = "SALARY" }
        };

        // Act
        var result = _sut.GroupTransactionsByCategory(transactions);

        // Assert
        result.Should().HaveCount(3);
        
        var groceries = result.First(c => c.Category == "Groceries");
        groceries.TotalAmount.Should().Be(-300);
        groceries.TransactionCount.Should().Be(2);
        groceries.AverageTransactionAmount.Should().Be(-150);

        var restaurants = result.First(c => c.Category == "Restaurants");
        restaurants.TotalAmount.Should().Be(-150);
        restaurants.TransactionCount.Should().Be(1);

        var income = result.First(c => c.Category == "Income");
        income.TotalAmount.Should().Be(5000);
        income.TransactionCount.Should().Be(1);
    }

    [Fact]
    public void GroupTransactionsByCategory_CalculatesPercentagesCorrectly()
    {
        // Arrange
        var transactions = new List<TransactionResponse>
        {
            new() { TransactionId = 1, Amount = -500, MerchantCode = "5411" }, // 50% of spending
            new() { TransactionId = 2, Amount = -300, MerchantCode = "5812" }, // 30% of spending
            new() { TransactionId = 3, Amount = -200, MerchantCode = "5541" }, // 20% of spending
            new() { TransactionId = 4, Amount = 2000, Description = "SALARY" }
        };

        // Act
        var result = _sut.GroupTransactionsByCategory(transactions);

        // Assert
        var groceries = result.First(c => c.Category == "Groceries");
        groceries.PercentageOfTotal.Should().BeApproximately(50m, 0.1m);

        var restaurants = result.First(c => c.Category == "Restaurants");
        restaurants.PercentageOfTotal.Should().BeApproximately(30m, 0.1m);

        var transport = result.First(c => c.Category == "Transport");
        transport.PercentageOfTotal.Should().BeApproximately(20m, 0.1m);
    }

    [Fact]
    public void GroupTransactionsByCategory_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var transactions = new List<TransactionResponse>();

        // Act
        var result = _sut.GroupTransactionsByCategory(transactions);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GroupTransactionsByCategory_SortsByAbsoluteAmountDescending()
    {
        // Arrange
        var transactions = new List<TransactionResponse>
        {
            new() { TransactionId = 1, Amount = -100, MerchantCode = "5411" },
            new() { TransactionId = 2, Amount = -500, MerchantCode = "5812" },
            new() { TransactionId = 3, Amount = -300, MerchantCode = "5541" }
        };

        // Act
        var result = _sut.GroupTransactionsByCategory(transactions);

        // Assert
        result[0].Category.Should().Be("Restaurants"); // -500
        result[1].Category.Should().Be("Transport");   // -300
        result[2].Category.Should().Be("Groceries");   // -100
    }
}