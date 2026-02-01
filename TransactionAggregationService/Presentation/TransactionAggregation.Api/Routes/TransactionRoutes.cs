using Microsoft.AspNetCore.Mvc;
using TransactionAggregation.Core.Services;
using TransactionAggregation.Models.Common;

namespace TransactionAggregation.Api.Routes;

public static class TransactionRoutes
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/customers/{customerId:long}")
            .WithTags("Transactions")
            .WithApiVersionSet(apiVersionSet: versionSet)
            .WithOpenApi();

        group.MapGet("/transactions", GetCustomerTransactions)
            .WithName("GetCustomerTransactions")
            .WithSummary("Get all aggregated transactions for a customer")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status404NotFound);

        group.MapGet("/transactions/summary", GetTransactionSummary)
            .WithName("GetTransactionSummary")
            .WithSummary("Get transaction summary with category breakdowns")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status404NotFound);

        group.MapPost("/transactions/statements", GenerateTransactionStatement)
            .WithName("GenerateTransactionStatement")
            .WithSummary("Generate PDF statement for customer transactions")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces<object>(StatusCodes.Status503ServiceUnavailable)
            .Produces<object>(StatusCodes.Status504GatewayTimeout);
    }

    private static async Task<IResult> GetCustomerTransactions(
        [FromRoute] long customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromServices] ITransactionAggregationService aggregationService)
    {
        var result = await aggregationService.GetCustomerTransactionsAsync(
            customerId,
            fromDate,
            toDate);

        return HandleServiceResult(result);
    }

    private static async Task<IResult> GetTransactionSummary(
        [FromRoute] long customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromServices] ITransactionAggregationService aggregationService)
    {
        var result = await aggregationService.GetTransactionSummaryAsync(
            customerId,
            fromDate,
            toDate);

        return HandleServiceResult(result);
    }

    private static async Task<IResult> GenerateTransactionStatement(
        [FromRoute] long customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromServices] ITransactionAggregationService aggregationService)
    {
        var result = await aggregationService.GenerateTransactionStatementAsync(
            customerId,
            fromDate,
            toDate);

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Shared method to handle service result responses
    /// Reduces code duplication across endpoints
    /// </summary>
    private static IResult HandleServiceResult<T>(Result<T> result)
    {
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "API_UNAVAILABLE" or "DOCUMENT_SERVICE_UNAVAILABLE" => Results.Problem(
                    title: "Service Unavailable",
                    detail: result.ErrorMessage,
                    statusCode: StatusCodes.Status503ServiceUnavailable),

                "API_TIMEOUT" or "DOCUMENT_SERVICE_TIMEOUT" => Results.Problem(
                    title: "Gateway Timeout",
                    detail: result.ErrorMessage,
                    statusCode: StatusCodes.Status504GatewayTimeout),

                "EMPTY_RESPONSE" => Results.NotFound(new
                {
                    error = result.ErrorMessage,
                    errorCode = result.ErrorCode
                }),

                _ => Results.BadRequest(new
                {
                    error = result.ErrorMessage,
                    errorCode = result.ErrorCode
                })
            };
        }

        // Check for warnings (stale cache, partial data, etc.)
        if (!string.IsNullOrEmpty(result.WarningMessage))
        {
            return Results.Ok(new
            {
                data = result.Data,
                warning = result.WarningMessage,
                warningCode = result.WarningCode
            });
        }

        return Results.Ok(result.Data);
    }
}