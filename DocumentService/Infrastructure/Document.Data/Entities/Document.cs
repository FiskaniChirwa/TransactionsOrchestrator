using Document.Models.Enums;

namespace Document.Data.Entities;

public class Document
{
    public Guid Id { get; set; }
    public DocumentType DocumentType { get; set; }
    
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    
    public string? CustomMetadata { get; set; }  // JSON string
    
    public string DownloadToken { get; set; } = string.Empty;
    public DateTime TokenExpiresAt { get; set; }
    public int TokenMaxUses { get; set; }
    public int TokenUseCount { get; set; }
    public bool IsTokenRevoked { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDownloadedAt { get; set; }
}