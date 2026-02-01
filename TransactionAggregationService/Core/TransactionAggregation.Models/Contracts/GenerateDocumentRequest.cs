namespace TransactionAggregation.Models.Contracts;

public class GenerateDocumentRequest
{
    public DocumentType DocumentType { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
    public DocumentOptions? Options { get; set; }
}

public class DocumentOptions
{
    public int? TokenExpiryMinutes { get; set; }
    public int? MaxDownloads { get; set; }
}

public enum DocumentType
{
    TransactionStatement = 1
}