using Document.Core.Services;
using Document.Infrastructure.Pdf;
using Document.Infrastructure.Services;
using Document.Models.Enums;
using Document.Models.Requests;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Document.Models.Common;

namespace Document.Tests.Unit.Infrastructure.Services;

public class DocumentServiceTests
{
    private readonly Mock<IPdfTemplateRegistry> _templateRegistryMock;
    private readonly Mock<IDocumentValidator> _validatorMock;
    private readonly Mock<IDocumentStorage> _storageMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _templateRegistryMock = new Mock<IPdfTemplateRegistry>();
        _validatorMock = new Mock<IDocumentValidator>();
        _storageMock = new Mock<IDocumentStorage>();
        _loggerMock = new Mock<ILogger<DocumentService>>();

        _sut = new DocumentService(
            _templateRegistryMock.Object,
            _validatorMock.Object,
            _storageMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateDocumentAsync_ShouldFollowWorkflow_AndReturnCorrectResponse()
    {
        // Arrange
        var request = new GenerateDocumentRequest
        {
            DocumentType = DocumentType.TransactionStatement,
            FileName = "statement.pdf",
            Data = new Dictionary<string, object> { { "key", "value" } },
            Options = new DocumentOptions { MaxDownloads = 10, TokenExpiryMinutes = 60 }
        };
        var baseUrl = "https://api.myapp.com";
        var mockStream = new MemoryStream();

        var templateMock = new Mock<IPdfTemplate>();
        templateMock.Setup(x => x.RenderAsync(request.Data)).ReturnsAsync(mockStream);
        _templateRegistryMock.Setup(x => x.GetTemplate(request.DocumentType)).Returns(templateMock.Object);

        var expectedId = Guid.NewGuid();
        var expectedToken = "secure-token-123";
        var expiresAt = DateTime.UtcNow.AddDays(1);

        _storageMock.Setup(x => x.StoreDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<DocumentType>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<int?>(),
            It.IsAny<int?>()))
            .ReturnsAsync((expectedId, expectedToken, expiresAt, "statement.pdf", 1024L));

        // Act
        var result = await _sut.GenerateDocumentAsync(request, baseUrl);

        // Assert
        result.Should().NotBeNull();
        result.DownloadUrl.Should().Be($"{baseUrl}/api/documents/{expectedId}/download?token={expectedToken}");
        result.DocumentId.Should().Be(expectedId);
        result.MaxDownloads.Should().Be(10);

        // Verify workflow order and execution
        _validatorMock.Verify(x => x.ValidateAsync(request.DocumentType, request.Data), Times.Once);
        _templateRegistryMock.Verify(x => x.GetTemplate(request.DocumentType), Times.Once);
        _storageMock.Verify(x => x.StoreDocumentAsync(It.IsAny<Stream>(), It.IsAny<DocumentType>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task GenerateDocumentAsync_ShouldThrow_WhenValidatorFails()
    {
        // Arrange
        var request = new GenerateDocumentRequest { DocumentType = DocumentType.TransactionStatement };
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<DocumentType>(), It.IsAny<Dictionary<string, object>>()))
            .ThrowsAsync(new InvalidOperationException("Validation Error"));

        // Act
        var act = () => _sut.GenerateDocumentAsync(request, "url");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Circuit break check: ensure subsequent heavy calls are skipped
        _templateRegistryMock.Verify(x => x.GetTemplate(It.IsAny<DocumentType>()), Times.Never);
        _storageMock.Verify(x => x.StoreDocumentAsync(It.IsAny<Stream>(), It.IsAny<DocumentType>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task GetSupportedDocumentTypesAsync_ShouldMapTemplateDataCorrectly()
    {
        // Arrange
        var type = DocumentType.TransactionStatement;
        _templateRegistryMock.Setup(x => x.GetSupportedTypes()).Returns(new List<DocumentType> { type });

        var templateMock = new Mock<IPdfTemplate>();
        templateMock.Setup(x => x.GetDescription()).Returns("Test Description");
        templateMock.Setup(x => x.GetRequiredFields()).Returns(new List<string> { "Field1" });
        templateMock.Setup(x => x.GetOptionalFields()).Returns(new List<string> { "Field2" });

        _templateRegistryMock.Setup(x => x.GetTemplate(type)).Returns(templateMock.Object);

        // Act
        var result = await _sut.GetSupportedDocumentTypesAsync();

        // Assert
        result.Should().HaveCount(1);
        var response = result.First();
        response.Type.Should().Be(type);
        response.Description.Should().Be("Test Description");
        response.RequiredFields.Should().Contain("Field1");
        response.OptionalFields.Should().Contain("Field2");
        response.ExampleData.Should().NotBeNull();
        response.ExampleData.Should().ContainKey("customerId");
    }

    [Fact]
    public async Task GenerateDocumentAsync_ShouldHandleCustomMetadata_WhenProvided()
    {
        // Arrange
        var customMeta = "Special Project A";
        var request = new GenerateDocumentRequest
        {
            DocumentType = DocumentType.TransactionStatement,
            Options = new DocumentOptions { CustomMetadata = customMeta }
        };

        var templateMock = new Mock<IPdfTemplate>();
        templateMock.Setup(x => x.RenderAsync(It.IsAny<Dictionary<string, object>>())).ReturnsAsync(new MemoryStream());
        _templateRegistryMock.Setup(x => x.GetTemplate(request.DocumentType)).Returns(templateMock.Object);
        _storageMock.Setup(x => x.StoreDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<DocumentType>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<int?>(),
            It.IsAny<int?>()))
            .ReturnsAsync((Guid.NewGuid(), "secure-token", DateTime.Now, "statement.pdf", 1024L));

        // Act
        var result = await _sut.GenerateDocumentAsync(request, "url");

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata["custom"].Should().Be(customMeta);
    }
}