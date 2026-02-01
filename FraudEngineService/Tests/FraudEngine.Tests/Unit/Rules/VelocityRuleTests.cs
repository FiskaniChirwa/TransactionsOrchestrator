using FluentAssertions;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Logging;
using Moq;

namespace FraudEngine.Tests.Unit.Rules;

public class VelocityRuleTests
{
    private readonly Mock<ITransactionHistoryRepository> _historyRepoMock = new();
    private readonly Mock<ILogger<VelocityRule>> _loggerMock = new();
    private readonly RuleConfiguration _config = new() { VelocityThreshold = 3, VelocityWindowMinutes = 10, VelocityRuleWeight = 50 };

    [Fact]
    public async Task EvaluateAsync_ShouldTrigger_WhenThresholdExceeded()
    {
        // Arrange
        var sut = new VelocityRule(_config, _historyRepoMock.Object, _loggerMock.Object);
        var tx = new Transaction { CustomerId = 1001, Category = "Shopping", TransactionDate = DateTime.Now };

        // Mock repository to say there are 2 existing transactions (2 + current 1 = 3, which hits threshold)
        _historyRepoMock.Setup(x => x.CountRecentTransactionsAsync(1001, "Shopping", It.IsAny<DateTime>()))
                        .ReturnsAsync(2);

        // Act
        var result = await sut.EvaluateAsync(tx);

        // Assert
        result.IsTriggered.Should().BeTrue();
        result.RiskScore.Should().Be(50);
        result.Reason.Should().Contain("3 transactions");
    }
}