using Document.Api.Extensions;
using Document.Api.Routes;
using Serilog;

LoggingExtension.ConfigureSerilog();

try
{
    Log.Information("Starting application...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddApiVersionings();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddCustomMiddleware();
    builder.Services.AddJsonSerialization();
    builder.Services.AddOutputCache();
    builder.Services.AddResponseCompression();
    builder.Services.AddRateLimiting(builder.Configuration);
    builder.Services.AddDocumentHealthChecks(builder.Configuration);

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDatabaseAndTemplates();
    app.UseCustomMiddleware();
    app.UseDocumentHealthChecks();
    app.UseHttpsRedirection();
    app.UseResponseCompression();
    app.UseRateLimiter();
    app.UseOutputCache();
    app.MapDocumentEndpoints();

    Log.Information("Document Service starting up...");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }