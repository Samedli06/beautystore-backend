using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.HasIndex(s => s.Key)
               .IsUnique();
               
        builder.Property(s => s.Key)
               .IsRequired()
               .HasMaxLength(100);
               
        builder.Property(s => s.Value)
               .IsRequired();
               
        builder.Property(s => s.Category)
               .HasMaxLength(50);
    }
}
