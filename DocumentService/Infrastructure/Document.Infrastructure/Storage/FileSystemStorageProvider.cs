using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Document.Infrastructure.Storage;

public class FileSystemStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<FileSystemStorageProvider> _logger;

    public FileSystemStorageProvider(
        IConfiguration configuration,
        ILogger<FileSystemStorageProvider> logger)
    {
        _basePath = configuration["Document:Storage:FileSystem:BasePath"]
            ?? throw new InvalidOperationException("FileSystem:BasePath not configured");

        _logger = logger;

        Directory.CreateDirectory(_basePath);

        _logger.LogInformation("FileSystem storage initialized at: {BasePath}", _basePath);
    }

    public async Task<string> StoreAsync(string key, Stream content)
    {
        var fullPath = Path.Combine(_basePath, key);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{fullPath}.tmp";

        try
        {
            await using (var fileStream = File.Create(tempPath))
            {
                await content.CopyToAsync(fileStream);
            }

            File.Move(tempPath, fullPath, overwrite: true);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store file at: {Key}", key);

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    public async Task<Stream> RetrieveAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {Key}", key);
            throw new FileNotFoundException($"Document file not found: {key}");
        }

        return await Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task<bool> ExistsAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {Key}", key);
        }

        return Task.CompletedTask;
    }
}