namespace TransactionAggregation.Models.Contracts;

public class GenerateDocumentResponse
{
    public Guid DocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string DownloadToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int MaxDownloads { get; set; }
    public DateTime GeneratedAt { get; set; }
}