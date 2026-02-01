using FraudEngine.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudEngine.Infrastructure.Data.Configurations;

public class FraudAlertConfiguration : IEntityTypeConfiguration<FraudAlertEntity>
{
    public void Configure(EntityTypeBuilder<FraudAlertEntity> builder)
    {
        builder.ToTable("FraudAlerts");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TransactionId);
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.CustomerId, e.CreatedAt });

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.MerchantName)
                .HasMaxLength(200)
                .IsRequired(false);

        builder.Property(e => e.MerchantCategory)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RulesTriggered)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired(false);
    }
}