using Asp.Versioning;
using Asp.Versioning.Builder;
using Document.Core.Services;
using Document.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Document.Api.Routes;

public static class DocumentRoutes
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .WithApiVersionSet(versionSet)
            .WithOpenApi();

        group.MapPost("/generate", GenerateDocument)
            .RequireRateLimiting("DocumentGenerationPolicy")
            .WithName("GenerateDocument")
            .WithSummary("Generate PDF document from data")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{documentId:guid}/download", DownloadDocument)
            .WithName("DownloadDocument")
            .WithSummary("Download PDF document using secure token")
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status401Unauthorized)
            .Produces<object>(StatusCodes.Status404NotFound);

        group.MapGet("/types", GetSupportedTypes)
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(60)))
            .WithName("GetSupportedDocumentTypes")
            .WithSummary("Get list of supported document types with required fields")
            .Produces<object>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GenerateDocument(
        [FromBody] GenerateDocumentRequest request,
        [FromServices] IDocumentService documentService,
        HttpContext httpContext)
    {
        try
        {
            if (request.Validate() is { IsValid: false, Message: var error })
            {
                return Results.BadRequest(new { error, errorCode = "INVALID_REQUEST_FORMAT" });
            }

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            var response = await documentService.GenerateDocumentAsync(request, baseUrl);

            return Results.Ok(response);
        }
        catch (Document.Core.Exceptions.DocumentValidationException ex)
        {
            return Results.BadRequest(new
            {
                error = "Validation failed",
                errorCode = "VALIDATION_ERROR",
                validationErrors = ex.ValidationErrors
            });
        }
        catch (Document.Core.Exceptions.TemplateNotFoundException ex)
        {
            return Results.BadRequest(new
            {
                error = ex.Message,
                errorCode = "TEMPLATE_NOT_FOUND",
                documentType = ex.DocumentType.ToString()
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Document Generation Failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> DownloadDocument(
        [FromRoute] Guid documentId,
        [FromQuery] string? token,
        [FromServices] IDocumentStorage storage)
    {
        if (documentId == Guid.Empty)
        {
            return Results.BadRequest(new
            {
                error = "Valid Document ID is required",
                errorCode = "INVALID_REQUEST_FORMAT"
            });
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Problem(
                title: "Token Required",
                detail: "Download token is required",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await storage.RetrieveDocumentAsync(documentId, token);

        if (result == null)
        {
            return Results.Problem(
                title: "Download Failed",
                detail: "Invalid token, expired link, or download limit reached",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (fileStream, fileName) = result.Value;

        return Results.File(
            fileStream,
            contentType: "application/pdf",
            fileDownloadName: fileName,
            enableRangeProcessing: true);
    }

    private static async Task<IResult> GetSupportedTypes(
        [FromServices] IDocumentService documentService)
    {
        var types = await documentService.GetSupportedDocumentTypesAsync();
        return Results.Ok(types);
    }
}