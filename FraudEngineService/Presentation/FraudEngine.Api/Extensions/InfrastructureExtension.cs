using FraudEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FraudEngine.Api.Extensions;

public static class InfrastructureExtensions
{
    public static IApplicationBuilder UseDatabase(this WebApplication app)
    {
        // Initialize database
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FraudEngineDbContext>();
            dbContext.Database.EnsureCreated();
            Log.Information("Database initialized");
        }

        return app;
    }
}