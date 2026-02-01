namespace Document.Models.Common;

public class DocumentOptions
{
    public int? TokenExpiryMinutes { get; set; }
    public int? MaxDownloads { get; set; }
    public string? CustomMetadata { get; set; }  // JSON string for extra tracking info
}