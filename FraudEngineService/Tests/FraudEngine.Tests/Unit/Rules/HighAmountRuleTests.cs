using FluentAssertions;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Logging;
using Moq;

namespace FraudEngine.Tests.Unit.Rules;

public class HighAmountRuleTests
{
    private readonly RuleConfiguration _config = new();

    [Fact]
    public async Task EvaluateAsync_ShouldUseDefaultThreshold_WhenCategoryMissing()
    {
        // Arrange
        var sut = new HighAmountRule(_config, new Mock<ILogger<HighAmountRule>>().Object);
        var tx = new Transaction { Category = "UnknownCategory", Amount = 50001m }; // Default is 5000

        // Act
        var result = await sut.EvaluateAsync(tx);

        // Assert
        result.IsTriggered.Should().BeTrue();
        result.Reason.Should().Contain("exceeds threshold");
    }
}