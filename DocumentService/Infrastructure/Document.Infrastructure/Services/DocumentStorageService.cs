using System.Security.Cryptography;
using Document.Core.Services;
using Document.Data;
using Document.Infrastructure.Storage;
using Document.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Document.Infrastructure.Services;

public class DocumentStorageService : IDocumentStorage
{
    private readonly DocumentDbContext _dbContext;
    private readonly IStorageProvider _storageProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentStorageService> _logger;
    
    public DocumentStorageService(
        DocumentDbContext dbContext,
        IStorageProvider storageProvider,
        IConfiguration configuration,
        ILogger<DocumentStorageService> logger)
    {
        _dbContext = dbContext;
        _storageProvider = storageProvider;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<(Guid DocumentId, string Token, DateTime ExpiresAt, string FileName, long FileSize)> 
        StoreDocumentAsync(
            Stream pdfStream,
            DocumentType documentType,
            string? fileName,
            Dictionary<string, object>? metadata,
            int? expiryMinutes,
            int? maxDownloads)
    {
        var documentId = Guid.NewGuid();
        var token = GenerateSecureToken();
        fileName ??= GenerateFileName(documentType, documentId);
        var storageKey = GenerateStorageKey(documentType, documentId);
        
        // Read stream and calculate checksum
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms);
        var pdfBytes = ms.ToArray();
        var checksum = CalculateSha256(pdfBytes);
        var fileSize = pdfBytes.Length;
        
        // Store file
        using var uploadStream = new MemoryStream(pdfBytes);
        await _storageProvider.StoreAsync(storageKey, uploadStream);
        
        // Get configuration
        var tokenExpiryMinutes = expiryMinutes 
            ?? _configuration.GetValue<int>("Document:Security:DefaultTokenExpiryMinutes", 60);
        var tokenMaxDownloads = maxDownloads 
            ?? _configuration.GetValue<int>("Document:Security:DefaultMaxDownloads", 5);
        var expiresAt = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes);
        
        // Serialize metadata if provided
        string? metadataJson = null;
        if (metadata != null && metadata.Any())
        {
            metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        }
        
        // Save to database
        var document = new Data.Entities.Document
        {
            Id = documentId,
            DocumentType = documentType,
            StorageKey = storageKey,
            FileName = fileName,
            FileSizeBytes = fileSize,
            Checksum = checksum,
            CustomMetadata = metadataJson,
            DownloadToken = token,
            TokenExpiresAt = expiresAt,
            TokenMaxUses = tokenMaxDownloads,
            TokenUseCount = 0,
            IsTokenRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Stored {DocumentType} document {DocumentId}, size: {FileSize} bytes",
            documentType,
            documentId,
            fileSize);
        
        return (documentId, token, expiresAt, fileName, fileSize);
    }
    
    public async Task<(Stream FileStream, string FileName)?> RetrieveDocumentAsync(
        Guid documentId, 
        string token)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DownloadToken == token);
        
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found or token invalid", documentId);
            return null;
        }
        
        // Validate token
        if (document.IsTokenRevoked)
        {
            _logger.LogWarning("Token for document {DocumentId} is revoked", documentId);
            return null;
        }
        
        if (document.TokenExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Token for document {DocumentId} has expired", documentId);
            return null;
        }
        
        if (document.TokenUseCount >= document.TokenMaxUses)
        {
            _logger.LogWarning(
                "Download limit reached for document {DocumentId} ({UseCount}/{MaxUses})",
                documentId,
                document.TokenUseCount,
                document.TokenMaxUses);
            return null;
        }
        
        // Retrieve file from storage
        var fileStream = await _storageProvider.RetrieveAsync(document.StorageKey);
        
        // Increment use count
        document.TokenUseCount++;
        document.LastDownloadedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Document {DocumentId} downloaded ({UseCount}/{MaxUses})",
            documentId,
            document.TokenUseCount,
            document.TokenMaxUses);
        
        return (fileStream, document.FileName);
    }
    
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
    
    private static string GenerateFileName(DocumentType documentType, Guid documentId)
    {
        var prefix = documentType.GetDefaultFilePrefix();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return $"{prefix}_{timestamp}_{documentId:N}.pdf";
    }
    
    private static string GenerateStorageKey(DocumentType documentType, Guid documentId)
    {
        var typeFolder = documentType.ToString().ToLowerInvariant();
        var yearMonth = DateTime.UtcNow.ToString("yyyy-MM");
        return $"{typeFolder}/{yearMonth}/{documentId:N}.pdf";
    }
    
    private static string CalculateSha256(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}