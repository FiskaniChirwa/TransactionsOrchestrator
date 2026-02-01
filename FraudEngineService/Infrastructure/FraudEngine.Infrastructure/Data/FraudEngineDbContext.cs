using FraudEngine.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Data;

public class FraudEngineDbContext : DbContext
{
    public FraudEngineDbContext(DbContextOptions<FraudEngineDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProcessedEventEntity> ProcessedEvents => Set<ProcessedEventEntity>();
    public DbSet<FraudAlertEntity> FraudAlerts => Set<FraudAlertEntity>();
    public DbSet<TransactionHistoryEntity> TransactionHistory => Set<TransactionHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FraudEngineDbContext).Assembly);
    }
}