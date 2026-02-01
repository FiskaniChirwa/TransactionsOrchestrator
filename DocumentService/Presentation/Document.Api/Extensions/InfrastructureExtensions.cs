using Document.Core.Services;
using Document.Data;
using Document.Infrastructure.Pdf;
using Document.Infrastructure.Pdf.Templates;
using Document.Infrastructure.Services;
using Document.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Document.Api.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<DocumentDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DocumentDb")));

        // Storage Provider
        var storageProvider = configuration["Document:Storage:Provider"] ?? "FileSystem";
        services.AddScoped<IStorageProvider, FileSystemStorageProvider>();

        // PDF Template Registry
        services.AddSingleton<IPdfTemplateRegistry, PdfTemplateRegistry>();

        // Register PDF Templates
        services.AddSingleton<IPdfTemplate, TransactionStatementTemplate>();

        return services;
    }


    public static IApplicationBuilder UseDatabaseAndTemplates(this WebApplication app)
    {

        // Initialize database
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            dbContext.Database.EnsureCreated();
            Log.Information("Database initialized");

            // Register templates
            var registry = scope.ServiceProvider.GetRequiredService<IPdfTemplateRegistry>();
            var templates = scope.ServiceProvider.GetServices<IPdfTemplate>();
            foreach (var template in templates)
            {
                registry.RegisterTemplate(template);
            }
            Log.Information("Registered {Count} PDF template(s)", templates.Count());
        }

        // Ensure storage directory exists
        var statementPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "documents");
        Directory.CreateDirectory(statementPath);
        Log.Information("Document storage directory: {Path}", statementPath);
        return app;
    }
}