using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class InstallmentOptionConfiguration : IEntityTypeConfiguration<InstallmentOption>
{
    public void Configure(EntityTypeBuilder<InstallmentOption> builder)
    {
        builder.ToTable("InstallmentOptions");
        
        builder.HasKey(io => io.Id);
        
        builder.Property(io => io.BankName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(io => io.InstallmentPeriod)
            .IsRequired();
        
        builder.Property(io => io.InterestPercentage)
            .HasColumnType("decimal(5,2)")
            .IsRequired();
        
        builder.Property(io => io.IsActive)
            .IsRequired();
        
        builder.Property(io => io.MinimumAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);
        
        builder.Property(io => io.DisplayOrder)
            .IsRequired();
        
        builder.Property(io => io.CreatedAt)
            .IsRequired();
        
        builder.Property(io => io.UpdatedAt)
            .IsRequired(false);
        
        // Index for better query performance
        builder.HasIndex(io => new { io.IsActive, io.DisplayOrder });
    }
}
