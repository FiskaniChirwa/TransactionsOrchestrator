using Document.Models.Enums;

namespace Document.Core.Services;

public interface IDocumentStorage
{
    Task<(Guid DocumentId, string Token, DateTime ExpiresAt, string FileName, long FileSize)> 
        StoreDocumentAsync(
            Stream pdfStream,
            DocumentType documentType,
            string? fileName,
            Dictionary<string, object>? metadata,
            int? expiryMinutes,
            int? maxDownloads);
    
    Task<(Stream FileStream, string FileName)?> RetrieveDocumentAsync(
        Guid documentId,
        string token);
}