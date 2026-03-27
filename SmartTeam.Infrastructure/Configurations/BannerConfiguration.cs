using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.MobileImageUrl)
            .HasMaxLength(500);

        builder.Property(b => b.LinkUrl)
            .HasMaxLength(500);

        builder.Property(b => b.ButtonText)
            .HasMaxLength(100);

        builder.Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        builder.Property(b => b.SortOrder)
            .HasDefaultValue(0);

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Style fields
        builder.Property(b => b.TitleColor).HasMaxLength(20).HasDefaultValue("#ffffff");
        builder.Property(b => b.TitleAlign).HasMaxLength(20).HasDefaultValue("center");
        builder.Property(b => b.DescriptionColor).HasMaxLength(20).HasDefaultValue("#eeeeee");
        builder.Property(b => b.ButtonColor).HasMaxLength(20).HasDefaultValue("#ffffff");
        builder.Property(b => b.ButtonTextColor).HasMaxLength(20).HasDefaultValue("#000000");

        builder.Property(b => b.TitlePositionX).HasDefaultValue(50);
        builder.Property(b => b.TitlePositionY).HasDefaultValue(20);
        builder.Property(b => b.TitleFontSize).HasDefaultValue(32);
        builder.Property(b => b.DescriptionPositionX).HasDefaultValue(50);
        builder.Property(b => b.DescriptionPositionY).HasDefaultValue(40);
        builder.Property(b => b.DescriptionFontSize).HasDefaultValue(16);
        builder.Property(b => b.ButtonPositionX).HasDefaultValue(50);
        builder.Property(b => b.ButtonPositionY).HasDefaultValue(65);
        builder.Property(b => b.ButtonBorderRadius).HasDefaultValue(8);

        // Indexes
        builder.HasIndex(b => new { b.Type, b.IsActive });
        builder.HasIndex(b => new { b.StartDate, b.EndDate });
    }
}

