namespace Document.Infrastructure.Storage;

public interface IStorageProvider
{
    Task<string> StoreAsync(string key, Stream content);
    Task<Stream> RetrieveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task DeleteAsync(string key);
}