using Document.Core.Exceptions;
using Document.Core.Services;
using Document.Infrastructure.Pdf;
using Document.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Document.Infrastructure.Services;

public class DocumentValidator(
    IPdfTemplateRegistry templateRegistry,
    ILogger<DocumentValidator> logger) : IDocumentValidator
{
    private readonly IPdfTemplateRegistry _templateRegistry = templateRegistry;
    private readonly ILogger<DocumentValidator> _logger = logger;

    public Task ValidateAsync(DocumentType type, Dictionary<string, object> data)
    {
        var errors = new List<string>();

        // Get template to check required fields
        IPdfTemplate template;
        try
        {
            template = _templateRegistry.GetTemplate(type);
        }
        catch (TemplateNotFoundException ex)
        {
            _logger.LogError(ex, "Template not found for {DocumentType}", type);
            throw new DocumentValidationException($"Unsupported document type: {type}");
        }

        // Validate required fields
        var requiredFields = template.GetRequiredFields();
        foreach (var field in requiredFields)
        {
            if (!data.ContainsKey(field))
            {
                errors.Add($"Required field '{field}' is missing");
            }
            else if (data[field] == null)
            {
                errors.Add($"Required field '{field}' cannot be null");
            }
        }

        // Additional validation for specific types
        switch (type)
        {
            case DocumentType.TransactionStatement:
                ValidateTransactionStatement(data, errors);
                break;
        }

        if (errors.Any())
        {
            _logger.LogWarning(
                "Validation failed for {DocumentType}: {Errors}",
                type,
                string.Join(", ", errors));

            throw new DocumentValidationException(errors);
        }

        return Task.CompletedTask;
    }

    private static void ValidateTransactionStatement(Dictionary<string, object> data, List<string> errors)
    {
        // Validate customerName
        if (data.TryGetValue("customerName", out var customerNameObj))
        {
            if (customerNameObj is string name && string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Field 'customerName' cannot be empty");
            }
        }

        // Validate accounts exists and is an array
        if (data.TryGetValue("accounts", out var accountsObj))
        {
            if (accountsObj is not System.Text.Json.JsonElement accountsJson ||
                accountsJson.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                errors.Add("Field 'accounts' must be an array");
            }
        }
    }
}