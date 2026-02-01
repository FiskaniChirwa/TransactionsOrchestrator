using Document.Core.Services;
using Document.Models.Enums;

namespace Document.Infrastructure.Pdf;

public interface IPdfTemplateRegistry
{
    void RegisterTemplate(IPdfTemplate template);
    IPdfTemplate GetTemplate(DocumentType documentType);
    List<DocumentType> GetSupportedTypes();
}