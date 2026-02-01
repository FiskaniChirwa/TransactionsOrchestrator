using FraudEngine.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudEngine.Infrastructure.Data.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEventEntity>
{
    public void Configure(EntityTypeBuilder<ProcessedEventEntity> builder)
    {
        builder.ToTable("ProcessedEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.EventId)
            .IsUnique();

        builder.HasIndex(e => e.TransactionId);

        builder.Property(e => e.ProcessedAt)
            .IsRequired();

        builder.Property(e => e.Result)
            .IsRequired();
    }
}