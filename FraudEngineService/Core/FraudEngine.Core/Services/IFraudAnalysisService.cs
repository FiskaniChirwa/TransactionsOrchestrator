using FraudEngine.Core.Common;
using FraudEngine.Core.Models;
using FraudEngine.Models.Responses;

namespace FraudEngine.Core.Services;

public interface IFraudAnalysisService
{
    Task<Result<FraudAnalysisResponse>> AnalyzeAsync(Transaction transaction);
}