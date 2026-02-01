using Document.Core.Exceptions;
using Document.Core.Services;
using Document.Infrastructure.Pdf;
using Document.Infrastructure.Services;
using Document.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Document.Tests.Unit.Infrastructure.Services;

public class DocumentValidatorTests
{
    private readonly Mock<IPdfTemplateRegistry> _templateRegistryMock;
    private readonly Mock<ILogger<DocumentValidator>> _loggerMock;
    private readonly Mock<IPdfTemplate> _templateMock;
    private readonly DocumentValidator _sut; // System Under Test

    public DocumentValidatorTests()
    {
        _templateRegistryMock = new Mock<IPdfTemplateRegistry>();
        _loggerMock = new Mock<ILogger<DocumentValidator>>();
        _templateMock = new Mock<IPdfTemplate>();

        _sut = new DocumentValidator(_templateRegistryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowException_WhenTemplateNotFound()
    {
        // Arrange
        var docType = DocumentType.TransactionStatement;
        _templateRegistryMock.Setup(x => x.GetTemplate(docType))
            .Throws(new TemplateNotFoundException(docType));

        // Act
        var act = () => _sut.ValidateAsync(docType, new Dictionary<string, object>());

        // Assert
        await act.Should().ThrowAsync<DocumentValidationException>()
            .WithMessage($"Unsupported document type: {docType}");
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowException_WhenRequiredFieldsAreMissing()
    {
        // Arrange
        var docType = DocumentType.TransactionStatement;
        var data = new Dictionary<string, object> { { "ExistingField", "Value" } };

        _templateMock.Setup(x => x.GetRequiredFields()).Returns(["RequiredField"]);
        _templateRegistryMock.Setup(x => x.GetTemplate(docType)).Returns(_templateMock.Object);

        // Act
        var act = () => _sut.ValidateAsync(docType, data);

        // Assert
        var exception = await act.Should().ThrowAsync<DocumentValidationException>();
        exception.Which.Message.Should().Contain("Required field 'RequiredField' is missing");
    }

    [Fact]
    public async Task ValidateAsync_TransactionStatement_ShouldFail_WhenAccountsIsNotArray()
    {
        // Arrange
        var docType = DocumentType.TransactionStatement;
        var data = new Dictionary<string, object>
        {
            { "customerName", "Jane Doe" },
            { "accounts", "NotAnArray" } // String instead of JsonElement array
        };

        _templateMock.Setup(x => x.GetRequiredFields()).Returns(new List<string>());
        _templateRegistryMock.Setup(x => x.GetTemplate(docType)).Returns(_templateMock.Object);

        // Act
        var act = () => _sut.ValidateAsync(docType, data);

        // Assert
        var exception = await act.Should().ThrowAsync<DocumentValidationException>();
        exception.Which.Message.Should().Contain("Field 'accounts' must be an array");
    }

    [Fact]
    public async Task ValidateAsync_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var docType = DocumentType.TransactionStatement;
        var data = new Dictionary<string, object> { { "Amount", 100.00 } };

        _templateMock.Setup(x => x.GetRequiredFields()).Returns(["Amount"]);
        _templateRegistryMock.Setup(x => x.GetTemplate(docType)).Returns(_templateMock.Object);

        // Act
        var act = () => _sut.ValidateAsync(docType, data);

        // Assert
        await act.Should().NotThrowAsync();
    }
}