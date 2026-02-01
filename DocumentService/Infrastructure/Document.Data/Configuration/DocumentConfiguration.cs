using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Document.Data.Entities;

namespace Document.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Entities.Document>
{
    public void Configure(EntityTypeBuilder<Entities.Document> builder)
    {
        builder.ToTable("Documents");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.DocumentType)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(d => d.StorageKey)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(d => d.FileName)
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(d => d.Checksum)
            .HasMaxLength(64)
            .IsRequired();
        
        builder.Property(d => d.DownloadToken)
            .HasMaxLength(512)
            .IsRequired();
        
        builder.Property(d => d.CustomMetadata)
            .HasMaxLength(2000);
        
        builder.Property(d => d.CreatedAt)
            .IsRequired();
        
        // Indexes for performance
        builder.HasIndex(d => d.DownloadToken)
            .IsUnique();
        
        builder.HasIndex(d => d.DocumentType);
        
        builder.HasIndex(d => d.TokenExpiresAt)
            .HasFilter("IsTokenRevoked = 0");
        
        builder.HasIndex(d => d.CreatedAt);
    }
}