using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Clients;

public interface IDocumentApiClient
{
    Task<Result<GenerateDocumentResponse>> GenerateDocumentAsync(
        GenerateDocumentRequest request);
}