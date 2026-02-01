using FraudEngine.Core.Models;
using FraudEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FraudEngine.Tests.Unit.Services;

public class RiskCalculatorTests
{
    private readonly RiskCalculator _calculator;
    private readonly Mock<ILogger<RiskCalculator>> _loggerMock = new();

    public RiskCalculatorTests()
    {
        _calculator = new RiskCalculator(_loggerMock.Object);
    }

    [Theory]
    [InlineData(10, 20, 30)]    // Normal sum
    [InlineData(60, 50, 100)]   // Capped at 100
    [InlineData(0, 0, 0)]       // No score
    public void CalculateRiskScore_SumsAndCapsCorrectly(int score1, int score2, int expected)
    {
        // Arrange
        var rules = new List<RuleResult>
        {
            new() { IsTriggered = true, RiskScore = score1 },
            new() { IsTriggered = true, RiskScore = score2 },
            new() { IsTriggered = false, RiskScore = 99 } // Should be ignored
        };

        // Act
        var result = _calculator.CalculateRiskScore(rules);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, "Clear")]
    [InlineData(39, "Clear")]
    [InlineData(40, "Flagged")]
    [InlineData(69, "Flagged")]
    [InlineData(70, "Blocked")]
    [InlineData(100, "Blocked")]
    public void DetermineStatus_ReturnsCorrectCategory(int score, string expectedStatus)
    {
        // Act
        var status = _calculator.DetermineStatus(score);

        // Assert
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public void GenerateReason_CombinesMultipleReasons()
    {
        // Arrange
        var rules = new List<RuleResult>
        {
            new() { IsTriggered = true, Reason = "High Amount" },
            new() { IsTriggered = true, Reason = "New Location" },
            new() { IsTriggered = false, Reason = "Ignored" }
        };

        // Act
        var reason = _calculator.GenerateReason(rules);

        // Assert
        Assert.Equal("High Amount; New Location", reason);
    }

    [Fact]
    public void GenerateReason_WhenNoRulesTriggered_ReturnsDefault()
    {
        // Act
        var reason = _calculator.GenerateReason(new List<RuleResult>());

        // Assert
        Assert.Equal("No fraud indicators detected", reason);
    }
}