using Document.Models.Common;
using Document.Models.Enums;

namespace Document.Models.Requests;

public record Result(bool IsValid, string? Message = null);

public class GenerateDocumentRequest
{
    public DocumentType DocumentType { get; set; }
    public string? FileName { get; set; }  // Optional, auto-generated if null
    public Dictionary<string, object> Data { get; set; } = [];
    public DocumentOptions? Options { get; set; }

    public Result Validate() => this switch
    {
        { DocumentType: var type } when !Enum.IsDefined(typeof(DocumentType), type)
                => new(false, "Invalid DocumentType."),
        { Data.Count: 0 }
            => new(false, "Data is required for document generation"),

        _ => new(true)
    };
}