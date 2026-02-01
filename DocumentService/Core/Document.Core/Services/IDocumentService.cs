using Document.Models.Requests;
using Document.Models.Responses;

namespace Document.Core.Services;

public interface IDocumentService
{
    Task<GenerateDocumentResponse> GenerateDocumentAsync(
        GenerateDocumentRequest request,
        string baseUrl);
    
    Task<List<DocumentTypeResponse>> GetSupportedDocumentTypesAsync();
}