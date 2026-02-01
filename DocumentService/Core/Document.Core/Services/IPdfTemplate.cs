using Document.Models.Enums;

namespace Document.Core.Services;

public interface IPdfTemplate
{
    DocumentType SupportedType { get; }
    Task<Stream> RenderAsync(Dictionary<string, object> data);
    List<string> GetRequiredFields();
    List<string> GetOptionalFields();
    string GetDescription();
}