using Document.Core.Exceptions;
using Document.Core.Services;
using Document.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Document.Infrastructure.Pdf;

public class PdfTemplateRegistry(ILogger<PdfTemplateRegistry> logger) : IPdfTemplateRegistry
{
    private readonly Dictionary<DocumentType, IPdfTemplate> _templates = [];
    private readonly ILogger<PdfTemplateRegistry> _logger = logger;

    public void RegisterTemplate(IPdfTemplate template)
    {
        if (_templates.ContainsKey(template.SupportedType))
        {
            _logger.LogWarning(
                "Template for {DocumentType} already registered, replacing",
                template.SupportedType);
        }
        
        _templates[template.SupportedType] = template;
        
        _logger.LogInformation(
            "Registered PDF template for {DocumentType}",
            template.SupportedType);
    }
    
    public IPdfTemplate GetTemplate(DocumentType documentType)
    {
        if (!_templates.TryGetValue(documentType, out var template))
        {
            throw new TemplateNotFoundException(documentType);
        }
        
        return template;
    }
    
    public List<DocumentType> GetSupportedTypes()
    {
        return _templates.Keys.ToList();
    }
}