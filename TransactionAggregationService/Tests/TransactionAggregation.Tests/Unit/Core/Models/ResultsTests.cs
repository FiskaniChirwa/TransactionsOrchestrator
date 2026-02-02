using FluentAssertions;
using TransactionAggregation.Models.Common;
using Xunit;

namespace TransactionAggregation.UnitTests.Core.Models;

public class ResultTests
{
    [Fact]
    public void SuccessResult_CreatesSuccessfulResult()
    {
        // Arrange
        var data = "test data";

        // Act
        var result = Result<string>.SuccessResult(data);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(data);
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void FailureResult_CreatesFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";
        var errorCode = "ERROR_001";

        // Act
        var result = Result<string>.FailureResult(errorMessage, errorCode);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
        result.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void SuccessResultWithWarning_CreatesSuccessWithWarning()
    {
        // Arrange
        var data = "test data";
        var warningMessage = "Stale data";
        var warningCode = "WARN_001";

        // Act
        var result = Result<string>.SuccessResultWithWarning(data, warningMessage, warningCode);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(data);
        result.WarningMessage.Should().Be(warningMessage);
        result.WarningCode.Should().Be(warningCode);
        result.ErrorMessage.Should().BeNull();
    }
}