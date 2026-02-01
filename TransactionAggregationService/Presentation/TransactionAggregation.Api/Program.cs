using Serilog;
using TransactionAggregation.Api.Extensions;
using TransactionAggregation.Api.Routes;
using TransactionAggregation.Core.Extensions;
using TransactionAggregation.Infrastructure.Data;
using TransactionAggregation.Infrastructure.Extensions;

LoggingExtensions.ConfigureSerilog();

try
{
    Log.Information("Starting application...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    builder.Services.AddApiVersionings();
    builder.Services.AddResponseCompression();
    builder.Services.AddJsonSerialization();
    builder.Services.AddCustomMiddleware();
    builder.Services.AddApplicationServices();
    builder.Services.AddCustomHealthChecks();
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddExternalApiClients(builder.Configuration);

    var app = builder.Build();
    using (var scope = app.Services.CreateScope())
    {
        var outboxContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        await outboxContext.Database.EnsureCreatedAsync();
        Log.Information("Outbox database initialized");
    }
    app.UseCustomMiddleware();
    app.UseResponseCompression();
    app.UseSwaggerDocumentation();
    app.UseHttpsRedirection();
    app.MapApiRoutes();
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