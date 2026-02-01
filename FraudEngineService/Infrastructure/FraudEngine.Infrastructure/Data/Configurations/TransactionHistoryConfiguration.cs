using FraudEngine.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudEngine.Infrastructure.Data.Configurations;

public class TransactionHistoryConfiguration : IEntityTypeConfiguration<TransactionHistoryEntity>
{
    public void Configure(EntityTypeBuilder<TransactionHistoryEntity> builder)
    {
        builder.ToTable("TransactionHistory");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TransactionId);
        builder.HasIndex(e => new { e.CustomerId, e.TransactionDate });
        builder.HasIndex(e => new { e.CustomerId, e.MerchantCategory, e.TransactionDate });

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.MerchantCategory)
            .HasMaxLength(100);
    }
}