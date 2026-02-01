using FraudEngine.Core.Mapping;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Services;
using FraudEngine.Models.Events;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.Api.Routes;

public static class FraudRoutes
{
    public static void MapFraudEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fraud")
            .WithTags("Fraud")
            .WithOpenApi();

        group.MapPost("/analyze", AnalyzeTransaction)
            .RequireRateLimiting("FraudEnginePolicy")
            .WithName("AnalyzeTransaction")
            .WithSummary("Analyze a transaction for fraud");
    }

    private static async Task<IResult> AnalyzeTransaction(
        [FromHeader(Name = "X-Event-Id")] string? eventId,
        [FromBody] TransactionEvent transactionEvent,
        [FromServices] IProcessedEventRepository processedEventRepository,
        [FromServices] IFraudAnalysisService fraudAnalysisService,
        [FromServices] ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Results.BadRequest(new
            {
                error = "X-Event-Id header is required for idempotency",
                errorCode = "MISSING_EVENT_ID"
            });
        }

        logger.LogInformation(
            "Processing fraud analysis request: EventId={EventId}, TransactionId={TransactionId}",
            eventId,
            transactionEvent.TransactionId);

        // 1. Check idempotency
        var existingResult = await processedEventRepository.GetByEventIdAsync(eventId);

        if (existingResult != null)
        {
            logger.LogInformation(
                "Duplicate request detected: EventId={EventId}, returning cached result",
                eventId);

            return Results.Ok(existingResult);
        }

        // 2. Map API model to domain model
        var transaction = transactionEvent.ToDomain();

        // 3. Perform fraud analysis
        var result = await fraudAnalysisService.AnalyzeAsync(transaction);

        if (!result.Success)
        {
            logger.LogError(
                "Fraud analysis failed: EventId={EventId}, Error={Error}",
                eventId,
                result.ErrorMessage);

            return Results.Problem(
                title: "Analysis Failed",
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // 4. Store processed event for idempotency
        await processedEventRepository.AddAsync(eventId, transaction.TransactionId, result.Data!);

        logger.LogInformation(
            "Fraud analysis completed: EventId={EventId}, TransactionId={TransactionId}, RiskScore={RiskScore}, Status={Status}",
            eventId,
            transaction.TransactionId,
            result.Data!.RiskScore,
            result.Data!.Status);

        return Results.Ok(result.Data);
    }
}