using Document.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Document.Tests.Unit.Infrastructure.Storage;

public class FileSystemStorageProviderTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<FileSystemStorageProvider>> _loggerMock;
    private readonly FileSystemStorageProvider _sut;

    public FileSystemStorageProviderTests()
    {
        // Create a unique temp directory for this test run
        _testBasePath = Path.Combine(Path.GetTempPath(), $"DocumentTests_{Guid.NewGuid()}");

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["Document:Storage:FileSystem:BasePath"]).Returns(_testBasePath);

        _loggerMock = new Mock<ILogger<FileSystemStorageProvider>>();

        _sut = new FileSystemStorageProvider(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task StoreAsync_ShouldCreateFileAndReturnKey()
    {
        // Arrange
        var key = "subfolder/testfile.txt";
        var content = "Hello World";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var result = await _sut.StoreAsync(key, stream);

        // Assert
        result.Should().Be(key);
        var fullPath = Path.Combine(_testBasePath, key);
        File.Exists(fullPath).Should().BeTrue();

        var savedContent = await File.ReadAllTextAsync(fullPath);
        savedContent.Should().Be(content);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldReturnValidStream_WhenFileExists()
    {
        // Arrange
        var key = "existing.txt";
        var content = "Retrieve Me";
        var fullPath = Path.Combine(_testBasePath, key);
        await File.WriteAllTextAsync(fullPath, content);

        // Act
        using var resultStream = await _sut.RetrieveAsync(key);
        using var reader = new StreamReader(resultStream);
        var resultText = await reader.ReadToEndAsync();

        // Assert
        resultText.Should().Be(content);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldThrowFileNotFound_WhenFileMissing()
    {
        // Act
        var act = () => _sut.RetrieveAsync("nonexistent.txt");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var key = "exists_test.txt";
        await File.WriteAllTextAsync(Path.Combine(_testBasePath, key), "data");

        // Act & Assert
        (await _sut.ExistsAsync(key)).Should().BeTrue();
        (await _sut.ExistsAsync("missing.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        // Arrange
        var key = "delete_me.txt";
        var fullPath = Path.Combine(_testBasePath, key);
        await File.WriteAllTextAsync(fullPath, "data");

        // Act
        await _sut.DeleteAsync(key);

        // Assert
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenConfigIsMissing()
    {
        // Arrange
        _configurationMock.Setup(x => x["Document:Storage:FileSystem:BasePath"]).Returns((string)null!);

        // Act
        var act = () => new FileSystemStorageProvider(_configurationMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*not configured*");
    }

    public void Dispose()
    {
        // Cleanup the temp directory after tests complete
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}