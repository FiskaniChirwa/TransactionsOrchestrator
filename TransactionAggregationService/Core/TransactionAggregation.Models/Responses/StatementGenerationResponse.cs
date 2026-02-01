namespace TransactionAggregation.Models.Responses;

public class StatementGenerationResponse
{
    public Guid StatementId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int MaxDownloads { get; set; }
}