using Document.Core.Services;
using Document.Infrastructure.Services;

namespace Document.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentValidator, DocumentValidator>();
        services.AddScoped<IDocumentStorage, DocumentStorageService>();
        services.AddScoped<IDocumentService, DocumentService>();

        return services;
    }
}