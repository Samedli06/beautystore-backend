using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class InstallmentConfigurationConfiguration : IEntityTypeConfiguration<InstallmentConfiguration>
{
    public void Configure(EntityTypeBuilder<InstallmentConfiguration> builder)
    {
        builder.ToTable("InstallmentConfigurations");
        
        builder.HasKey(ic => ic.Id);
        
        builder.Property(ic => ic.IsEnabled)
            .IsRequired();
        
        builder.Property(ic => ic.MinimumAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(ic => ic.CreatedAt)
            .IsRequired();
        
        builder.Property(ic => ic.UpdatedAt)
            .IsRequired(false);
    }
}
