using FraudEngine.Api.Routes;
using FraudEngine.Api.HealthChecks;
using FraudEngine.Api.Extensions;
using FraudEngine.Core.Extensions;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Fraud Engine API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddFraudEngine();
    builder.Services.AddCustomHealthChecks();

    var app = builder.Build();

    app.UseDatabase();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
    app.MapFraudEndpoints();
    app.MapAlertEndpoints();
    app.MapHealthEndpoints();

    Log.Information("Fraud Engine API started successfully");
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