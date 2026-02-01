using Document.Models.Enums;

namespace Document.Core.Services;

public interface IDocumentValidator
{
    Task ValidateAsync(DocumentType type, Dictionary<string, object> data);
}