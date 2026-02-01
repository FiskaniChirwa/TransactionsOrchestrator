namespace Document.Models.Enums;

public enum DocumentType
{
    TransactionStatement = 1
}

public static class DocumentTypeExtensions
{
    public static string ToFriendlyName(this DocumentType type) => 
        type switch
        {
            DocumentType.TransactionStatement => "Transaction Statement",
            _ => type.ToString()
        };
    
    public static string GetDefaultFilePrefix(this DocumentType type) => 
        type switch
        {
            DocumentType.TransactionStatement => "statement",
            _ => type.ToString().ToLowerInvariant()
        };
}