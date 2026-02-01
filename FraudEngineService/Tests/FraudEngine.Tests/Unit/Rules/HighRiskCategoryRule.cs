using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace FraudEngine.Tests.Unit.Rules;

public class HighRiskCategoryRuleTests
{
    private readonly RuleConfiguration _config = new() 
    { 
        FirstTimeCategoryWeight = 10,
        HighRiskCategoryWeight = 40,
        HighRiskCategories = new List<string> { "Cryptocurrency", "Gambling" }
    };

    [Theory]
    [InlineData("Cryptocurrency", true, 40)]
    [InlineData("Groceries", false, 0)]
    public async Task HighRiskCategoryRule_ShouldEvaluateCorrectly(string category, bool expectedTrigger, int expectedScore)
    {
        // Arrange
        var sut = new HighRiskCategoryRule(_config, new Mock<ILogger<HighRiskCategoryRule>>().Object);
        var tx = new Transaction { Category = category };

        // Act
        var result = await sut.EvaluateAsync(tx);

        // Assert
        result.IsTriggered.Should().Be(expectedTrigger);
        result.RiskScore.Should().Be(expectedScore);
    }
}