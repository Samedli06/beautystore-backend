using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class PendingOrderConfiguration : IEntityTypeConfiguration<PendingOrder>
{
    public void Configure(EntityTypeBuilder<PendingOrder> builder)
    {
        builder.ToTable("PendingOrders");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.CartSnapshot)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
        
        builder.Property(p => p.CustomerInfo)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
        
        builder.Property(p => p.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(p => p.PromoCode)
            .HasMaxLength(50);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        builder.Property(p => p.ExpiresAt)
            .IsRequired();
        
        // Relationship with User
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Index for cleanup queries
        builder.HasIndex(p => p.ExpiresAt);
    }
}
