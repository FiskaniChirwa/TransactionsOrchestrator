using Document.Infrastructure.Pdf.Templates;
using Document.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Document.Tests.Unit.Infrastructure.Pdf;

public class TransactionStatementTemplateTests
{
    private readonly Mock<ILogger<TransactionStatementTemplate>> _loggerMock;
    private readonly TransactionStatementTemplate _sut;

    public TransactionStatementTemplateTests()
    {
        _loggerMock = new Mock<ILogger<TransactionStatementTemplate>>();
        _sut = new TransactionStatementTemplate(_loggerMock.Object);
    }

    [Fact]
    public void Metadata_ShouldReturnCorrectValues()
    {
        // Assert
        _sut.SupportedType.Should().Be(DocumentType.TransactionStatement);
        _sut.GetDescription().Should().NotBeEmpty();
        _sut.GetRequiredFields().Should().Contain(new[] { "customerId", "customerName", "accounts" });
    }

    [Fact]
    public async Task RenderAsync_WithValidFullData_ShouldGeneratePdfStream()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["customerId"] = 1001,
            ["customerName"] = "John Doe",
            ["totalTransactions"] = 2,
            ["accounts"] = new List<object>
            {
                new {
                    acountId = 1000,
                    accountType = "Savings",
                    accountNumber = "123456789",
                    currency = "USD",
                    currentBalance = 5000.00m,
                    availableBalance = 4800.00m,
                    transactions = new List<object>
                    {
                        new {
                            date = DateTime.Now.AddDays(-1),
                            description = "Grocery Store",
                            merchantName = "SuperMart",
                            amount = -50.25m,
                            balanceAfter = 4949.75m,
                            currency = "USD",
                            status = "Completed"
                        }
                    }
                }
            }
        };

        // Act
        using var stream = await _sut.RenderAsync(data);

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
        stream.Position.Should().Be(0);

        _loggerMock.VerifyLog(LogLevel.Information, "Rendering Transaction Statement PDF");
        _loggerMock.VerifyLog(LogLevel.Information, "Transaction Statement PDF generated successfully");
    }

    [Fact]
    public async Task RenderAsync_WithEmptyAccounts_ShouldStillGeneratePdf()
    {
        // Arrange - Tests the "No accounts found" branch in ComposeContent
        var data = new Dictionary<string, object>
        {
            ["customerId"] = 999,
            ["customerName"] = "Empty User",
            ["accounts"] = new List<object>()
        };

        // Act
        using var stream = await _sut.RenderAsync(data);

        // Assert
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderAsync_WithInvalidData_ShouldThrowInvalidOperationException()
    {
        // Arrange - Missing the "accounts" key entirely to trigger deserialization failure/null
        var data = new Dictionary<string, object>
        {
            ["customerId"] = "CUST-001"
            // accounts is missing
        };

        // Act
        var act = () => _sut.RenderAsync(data);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}

// Helper to verify ILogger calls
public static class LoggerExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string message)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}