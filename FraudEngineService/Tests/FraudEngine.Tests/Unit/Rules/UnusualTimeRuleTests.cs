using FluentAssertions;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Logging;
using Moq;

namespace FraudEngine.Tests.Unit.Rules;

public class UnusualTimeRuleTests
{
    private readonly RuleConfiguration _config = new() { UnusualHourStart = 23, UnusualHourEnd = 6, UnusualTimeRuleWeight = 15 };

    [Theory]
    [InlineData(2, "Shopping", true)]  // 2 AM Shopping = Triggered
    [InlineData(14, "Shopping", false)] // 2 PM Shopping = Safe
    [InlineData(2, "Salary", false)]    // 2 AM Salary = Safe (not in suspicious list)
    public async Task EvaluateAsync_ShouldHandleHoursCorrectly(int hour, string category, bool expectedTrigger)
    {
        // Arrange
        var sut = new UnusualTimeRule(_config, new Mock<ILogger<UnusualTimeRule>>().Object);
        var tx = new Transaction { 
            Category = category, 
            TransactionDate = new DateTime(2024, 1, 1, hour, 0, 0) 
        };

        // Act
        var result = await sut.EvaluateAsync(tx);

        // Assert
        result.IsTriggered.Should().Be(expectedTrigger);
    }
}