using System.Net;
using System.Net.Http.Json;
using Document.Core.Exceptions;
using Document.Core.Services;
using Document.Models.Enums;
using Document.Models.Requests;
using Document.Models.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;

namespace Document.Tests.Integration.Routes;

public class DocumentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IDocumentService> _documentServiceMock = new();
    private readonly Mock<IDocumentStorage> _storageMock = new();

    public DocumentApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with Mocks for controlled integration testing
                services.AddScoped(_ => _documentServiceMock.Object);
                services.AddScoped(_ => _storageMock.Object);
            });
        });
    }

    [Fact]
    public async Task GenerateDocument_ReturnsOk_WhenRequestIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new GenerateDocumentRequest
        {
            DocumentType = DocumentType.TransactionStatement,
            Data = new Dictionary<string, object> { ["customerId"] = 12345 }
        };

        var expectedGuid = Guid.NewGuid();
        var expectedResponse = new GenerateDocumentResponse { DocumentId = expectedGuid, DownloadToken = "abc" };
        
        _documentServiceMock.Setup(x => x.GenerateDocumentAsync(It.IsAny<GenerateDocumentRequest>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await client.PostAsJsonAsync("/api/documents/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GenerateDocumentResponse>();
        content!.DocumentId.Should().Be(expected: expectedGuid);
    }

    [Fact]
    public async Task GenerateDocument_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new GenerateDocumentRequest { DocumentType = DocumentType.TransactionStatement };
        
        _documentServiceMock.Setup(x => x.GenerateDocumentAsync(It.IsAny<GenerateDocumentRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new DocumentValidationException(new List<string> { "Field missing" }));

        // Act
        var response = await client.PostAsJsonAsync("/api/documents/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("INVALID_REQUEST_FORMAT");
    }

    [Fact]
    public async Task DownloadDocument_ReturnsFile_WhenTokenIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var docId = Guid.NewGuid();
        var token = "secret-token";
        var mockFileStream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF Magic Number
        
        _storageMock.Setup(x => x.RetrieveDocumentAsync(docId, token))
            .ReturnsAsync((mockFileStream, "test.pdf"));

        // Act
        var response = await client.GetAsync($"/api/documents/{docId}/download?token={token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task DownloadDocument_ReturnsUnauthorized_WhenTokenMissing()
    {
        // Arrange
        var client = _factory.CreateClient();
        var docId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/documents/{docId}/download"); // No token query string

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}