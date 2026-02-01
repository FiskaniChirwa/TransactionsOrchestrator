using Document.Models.Enums;

namespace Document.Models.Responses;

public class DocumentTypeResponse
{
    public DocumentType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredFields { get; set; } = [];
    public List<string> OptionalFields { get; set; } = [];
    public Dictionary<string, string>? ExampleData { get; set; }
}