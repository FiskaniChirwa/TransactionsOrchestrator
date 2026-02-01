using Document.Data;
using Document.Infrastructure.Services;
using Document.Infrastructure.Storage;
using Document.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Document.Tests.Unit.Infrastructure.Services;

public class DocumentStorageServiceTests
{
    private readonly DocumentDbContext _dbContext;
    private readonly Mock<IStorageProvider> _storageProviderMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<DocumentStorageService>> _loggerMock;
    private readonly DocumentStorageService _sut;

    public DocumentStorageServiceTests()
    {
        // Setup In-Memory DB
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new DocumentDbContext(options);

        _storageProviderMock = new Mock<IStorageProvider>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<DocumentStorageService>>();

        // Default Config
        _configurationMock.Setup(x => x.GetSection("Document:Security:DefaultTokenExpiryMinutes").Value).Returns("60");
        _configurationMock.Setup(x => x.GetSection("Document:Security:DefaultMaxDownloads").Value).Returns("5");

        _sut = new DocumentStorageService(
            _dbContext,
            _storageProviderMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task StoreDocumentAsync_ShouldSaveToDbAndStorage()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var type = DocumentType.TransactionStatement;

        // Act
        var result = await _sut.StoreDocumentAsync(stream, type, "test.pdf", null, null, null);

        // Assert
        // 1. Check DB
        var dbDoc = await _dbContext.Documents.FindAsync(result.DocumentId);
        dbDoc.Should().NotBeNull();
        dbDoc!.FileName.Should().Be("test.pdf");
        dbDoc.Checksum.Should().NotBeNullOrEmpty();

        // 2. Check Storage Provider call
        _storageProviderMock.Verify(x => x.StoreAsync(
            It.Is<string>(s => s.Contains(result.DocumentId.ToString("N"))),
            It.IsAny<Stream>()),
            Times.Once);
    }

    [Fact]
    public async Task RetrieveDocumentAsync_ShouldIncrementUseCount_AndReturnStream()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var token = "valid-token";
        var storageKey = "path/to/file.pdf";

        var doc = new Data.Entities.Document
        {
            Id = docId,
            DownloadToken = token,
            StorageKey = storageKey,
            TokenMaxUses = 5,
            TokenUseCount = 0,
            TokenExpiresAt = DateTime.UtcNow.AddHours(1),
            FileName = "file.pdf"
        };
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        _storageProviderMock.Setup(x => x.RetrieveAsync(storageKey))
            .ReturnsAsync(new MemoryStream());

        // Act
        var result = await _sut.RetrieveDocumentAsync(docId, token);

        // Assert
        result.Should().NotBeNull();
        doc.TokenUseCount.Should().Be(1);
        doc.LastDownloadedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RetrieveDocumentAsync_ShouldReturnNull_WhenTokenExpired()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var token = "expired-token";

        var doc = new Data.Entities.Document
        {
            Id = docId,
            DownloadToken = token,
            TokenExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            TokenMaxUses = 5,
            TokenUseCount = 0
        };
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RetrieveDocumentAsync(docId, token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveDocumentAsync_ShouldReturnNull_WhenMaxDownloadsReached()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var token = "maxed-token";

        var doc = new Data.Entities.Document
        {
            Id = docId,
            DownloadToken = token,
            TokenMaxUses = 1,
            TokenUseCount = 1, // Already used once
            TokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RetrieveDocumentAsync(docId, token);

        // Assert
        result.Should().BeNull();
    }
}