using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace FraudEngine.Tests.Unit.Rules;

public class FirstTimeCategoryRuleTests
{
    private readonly RuleConfiguration _config = new() 
    { 
        FirstTimeCategoryWeight = 10,
        HighRiskCategoryWeight = 40,
        HighRiskCategories = new List<string> { "Cryptocurrency", "Gambling" }
    };
    private readonly Mock<ITransactionHistoryRepository> _historyRepoMock = new();

    [Theory]
    [InlineData(true, 10)]  // First time = triggered
    [InlineData(false, 0)]  // Not first time = not triggered
    public async Task FirstTimeCategoryRule_ShouldEvaluateCorrectly(bool isFirstTime, int expectedScore)
    {
        // Arrange
        var sut = new FirstTimeCategoryRule(_config, _historyRepoMock.Object, new Mock<ILogger<FirstTimeCategoryRule>>().Object);
        var tx = new Transaction { CustomerId = 1001, Category = "Electronics" };
        
        _historyRepoMock.Setup(x => x.IsFirstTimeCategoryAsync(1001, "Electronics")).ReturnsAsync(isFirstTime);

        // Act
        var result = await sut.EvaluateAsync(tx);

        // Assert
        result.IsTriggered.Should().Be(isFirstTime);
        result.RiskScore.Should().Be(expectedScore);
    }
}