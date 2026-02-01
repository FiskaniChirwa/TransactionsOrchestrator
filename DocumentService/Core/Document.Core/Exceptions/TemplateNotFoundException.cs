using Document.Models.Enums;

namespace Document.Core.Exceptions;

public class TemplateNotFoundException(DocumentType documentType) 
    : Exception($"No template registered for document type: {documentType}")
{
    public DocumentType DocumentType { get; } = documentType;
}