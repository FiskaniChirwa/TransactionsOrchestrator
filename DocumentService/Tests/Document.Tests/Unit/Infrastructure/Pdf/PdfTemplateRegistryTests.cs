using Document.Core.Exceptions;
using Document.Infrastructure.Pdf;
using Document.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Document.Core.Services;

namespace Document.Tests.Unit.Infrastructure.Pdf;

public class PdfTemplateRegistryTests
{
    private readonly Mock<ILogger<PdfTemplateRegistry>> _loggerMock;
    private readonly PdfTemplateRegistry _sut;

    public PdfTemplateRegistryTests()
    {
        _loggerMock = new Mock<ILogger<PdfTemplateRegistry>>();
        _sut = new PdfTemplateRegistry(_loggerMock.Object);
    }

    [Fact]
    public void RegisterTemplate_ShouldAddAndRetrieveTemplate()
    {
        // Arrange
        var templateMock = new Mock<IPdfTemplate>();
        templateMock.Setup(x => x.SupportedType).Returns(DocumentType.TransactionStatement);

        // Act
        _sut.RegisterTemplate(templateMock.Object);

        // Assert
        var result = _sut.GetTemplate(DocumentType.TransactionStatement);
        result.Should().Be(templateMock.Object);
    }

    [Fact]
    public void RegisterTemplate_ShouldOverwrite_WhenSameTypeIsRegisteredTwice()
    {
        // Arrange
        var type = DocumentType.TransactionStatement;

        var firstMock = new Mock<IPdfTemplate>();
        firstMock.Setup(x => x.SupportedType).Returns(type);

        var secondMock = new Mock<IPdfTemplate>();
        secondMock.Setup(x => x.SupportedType).Returns(type);

        // Act
        _sut.RegisterTemplate(firstMock.Object);
        _sut.RegisterTemplate(secondMock.Object); // Same type, different instance

        // Assert
        var activeTemplate = _sut.GetTemplate(type);
        activeTemplate.Should().Be(secondMock.Object);
        activeTemplate.Should().NotBe(firstMock.Object);
    }

    [Fact]
    public void GetTemplate_ShouldThrowException_WhenTypeIsNotRegistered()
    {
        // Act
        var act = () => _sut.GetTemplate(DocumentType.TransactionStatement);

        // Assert
        act.Should().Throw<TemplateNotFoundException>();
    }
}