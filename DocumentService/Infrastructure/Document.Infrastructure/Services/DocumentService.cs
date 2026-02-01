using Document.Core.Services;
using Document.Infrastructure.Pdf;
using Document.Models.Enums;
using Document.Models.Requests;
using Document.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Document.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IPdfTemplateRegistry _templateRegistry;
    private readonly IDocumentValidator _validator;
    private readonly IDocumentStorage _storage;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IPdfTemplateRegistry templateRegistry,
        IDocumentValidator validator,
        IDocumentStorage storage,
        ILogger<DocumentService> logger)
    {
        _templateRegistry = templateRegistry;
        _validator = validator;
        _storage = storage;
        _logger = logger;
    }

    public async Task<GenerateDocumentResponse> GenerateDocumentAsync(
        GenerateDocumentRequest request,
        string baseUrl)
    {
        _logger.LogInformation(
            "Generating {DocumentType} document",
            request.DocumentType);

        // 1. Validate request data
        await _validator.ValidateAsync(request.DocumentType, request.Data);

        // 2. Get template
        var template = _templateRegistry.GetTemplate(request.DocumentType);

        // 3. Generate PDF
        var pdfStream = await template.RenderAsync(request.Data);

        // 4. Store PDF and get download token
        var (documentId, token, expiresAt, fileName, fileSize) =
            await _storage.StoreDocumentAsync(
                pdfStream,
                request.DocumentType,
                request.FileName,
                request.Data, // Store original data as metadata
                request.Options?.TokenExpiryMinutes,
                request.Options?.MaxDownloads);

        // 5. Build download URL
        var downloadUrl = $"{baseUrl}/api/documents/{documentId}/download?token={token}";

        _logger.LogInformation(
            "{DocumentType} document {DocumentId} generated successfully",
            request.DocumentType,
            documentId);

        // 6. Return response
        return new GenerateDocumentResponse
        {
            DocumentId = documentId,
            DocumentType = request.DocumentType,
            FileName = fileName,
            FileSizeBytes = fileSize,
            DownloadUrl = downloadUrl,
            DownloadToken = token,
            ExpiresAt = expiresAt,
            MaxDownloads = request.Options?.MaxDownloads ?? 5,
            GeneratedAt = DateTime.UtcNow,
            Metadata = request.Options?.CustomMetadata != null
                ? new Dictionary<string, object> { ["custom"] = request.Options.CustomMetadata }
                : null
        };
    }

    public Task<List<DocumentTypeResponse>> GetSupportedDocumentTypesAsync()
    {
        var supportedTypes = _templateRegistry.GetSupportedTypes();
        var responses = supportedTypes.Select(type =>
        {
            var template = _templateRegistry.GetTemplate(type);
            return new DocumentTypeResponse
            {
                Type = type,
                Name = type.ToFriendlyName(),
                Description = template.GetDescription(),
                RequiredFields = template.GetRequiredFields(),
                OptionalFields = template.GetOptionalFields(),
                ExampleData = GetExampleData(type)
            };
        }).ToList();

        return Task.FromResult(responses);
    }

    private static Dictionary<string, string>? GetExampleData(DocumentType type)
    {
        return type switch
        {
            DocumentType.TransactionStatement => new Dictionary<string, string>
            {
                ["customerId"] = "123",
                ["customerName"] = "John Doe",
                ["accounts"] = "[{ \"accountId\": 456, \"accountType\": \"Checking\", ... }]",
                ["totalTransactions"] = "25",
                ["dateRange"] = "{ \"fromDate\": \"2024-01-01\", \"toDate\": \"2024-01-31\" }"
            },
            _ => null
        };
    }
}