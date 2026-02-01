using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;
using FraudEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace FraudEngine.Tests.Unit.Services;

public class FraudAnalysisServiceTests
{
    private readonly Mock<IRiskCalculator> _riskCalcMock = new();
    private readonly Mock<IFraudAlertRepository> _alertRepoMock = new();
    private readonly Mock<ITransactionHistoryRepository> _historyRepoMock = new();
    private readonly Mock<ILogger<FraudAnalysisService>> _loggerMock = new();
    private readonly List<Mock<IFraudRule>> _ruleMocks = new();
    private readonly FraudAnalysisService _sut;

    public FraudAnalysisServiceTests()
    {
        // Setup two mock rules
        var rule1 = new Mock<IFraudRule>();
        rule1.Setup(r => r.RuleName).Returns("Rule1");
        rule1.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
             .ReturnsAsync(new RuleResult { IsTriggered = true, RiskScore = 20 });

        var rule2 = new Mock<IFraudRule>();
        rule2.Setup(r => r.RuleName).Returns("Rule2");
        rule2.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
             .ReturnsAsync(new RuleResult { IsTriggered = false, RiskScore = 0 });

        _ruleMocks.Add(rule1);
        _ruleMocks.Add(rule2);

        _sut = new FraudAnalysisService(
            _ruleMocks.Select(m => m.Object),
            _riskCalcMock.Object,
            _alertRepoMock.Object,
            _historyRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSucceed_EvenIfOneRuleFails()
    {
        // Arrange
        var transaction = new Transaction { TransactionId = 1001, Amount = 100 };
        
        // Make rule1 crash to test the internal try-catch block
        _ruleMocks[0].Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
                     .ThrowsAsync(new Exception("Rule engine failure"));

        _riskCalcMock.Setup(x => x.CalculateRiskScore(It.IsAny<List<RuleResult>>())).Returns(10);
        _riskCalcMock.Setup(x => x.DetermineStatus(It.IsAny<int>())).Returns("Low");

        // Act
        var result = await _sut.AnalyzeAsync(transaction);

        // Assert
        result.Success.Should().BeTrue();
        // Verify history was still added despite a rule failure
        _historyRepoMock.Verify(x => x.AddAsync(transaction), Times.Once);
        _alertRepoMock.Verify(x => x.AddAsync(It.IsAny<Alert>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnFailure_OnGlobalException()
    {
        // Arrange
        _riskCalcMock.Setup(x => x.CalculateRiskScore(It.IsAny<List<RuleResult>>()))
                     .Throws(new Exception("Database down"));

        // Act
        var result = await _sut.AnalyzeAsync(new Transaction());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ANALYSIS_ERROR");
    }
}