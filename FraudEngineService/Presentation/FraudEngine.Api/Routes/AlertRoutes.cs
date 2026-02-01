using FraudEngine.Core.Mapping;
using FraudEngine.Core.Repositories;
using FraudEngine.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.Api.Routes;

public static class AlertRoutes
{
    public static void MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fraud/alerts")
            .WithTags("Alerts")
            .WithOpenApi();

        group.MapGet("", GetAlerts)
            .RequireRateLimiting("FraudEnginePolicy")
            .WithName("GetAlerts")
            .WithSummary("Get fraud alerts with optional customer filter");

        group.MapGet("/{id:long}", GetAlertById)
            .WithName("GetAlertById")
            .WithSummary("Get a specific fraud alert by ID");

        group.MapGet("/transaction/{transactionId:long}", GetAlertByTransactionId)
            .WithName("GetAlertByTransactionId")
            .WithSummary("Get fraud alert by transaction ID");
    }

    private static async Task<IResult> GetAlerts(
        [FromServices] IFraudAlertRepository alertRepository,
        [FromQuery] long? customerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (customerId.HasValue)
        {
            var customerAlerts = await alertRepository.GetByCustomerIdAsync(
                customerId.Value,
                fromDate,
                toDate);

            var customerAlertResponses = customerAlerts.Select(a => a.ToResponse()).ToList();

            return Results.Ok(new AlertListResponse
            {
                Data = customerAlertResponses,
                Count = customerAlertResponses.Count,
                Skip = 0,
                Take = customerAlertResponses.Count,
                CustomerId = customerId
            });
        }

        var alerts = await alertRepository.GetAllAsync(skip, take);
        var alertResponses = alerts.Select(a => a.ToResponse()).ToList();

        return Results.Ok(new AlertListResponse
        {
            Data = alertResponses,
            Count = alertResponses.Count,
            Skip = skip,
            Take = take
        });
    }

    private static async Task<IResult> GetAlertById(
        [FromRoute] long id,
        [FromServices] IFraudAlertRepository alertRepository)
    {
        var alert = await alertRepository.GetByIdAsync(id);

        if (alert == null)
        {
            return Results.NotFound(new
            {
                error = $"Alert with ID {id} not found",
                errorCode = "ALERT_NOT_FOUND"
            });
        }

        return Results.Ok(alert.ToResponse());
    }

    private static async Task<IResult> GetAlertByTransactionId(
        [FromRoute] long transactionId,
        [FromServices] IFraudAlertRepository alertRepository)
    {
        var alert = await alertRepository.GetByTransactionIdAsync(transactionId);

        if (alert == null)
        {
            return Results.NotFound(new
            {
                error = $"Alert for transaction {transactionId} not found",
                errorCode = "ALERT_NOT_FOUND"
            });
        }

        return Results.Ok(alert.ToResponse());
    }
}