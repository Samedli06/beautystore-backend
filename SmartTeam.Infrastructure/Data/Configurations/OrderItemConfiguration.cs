using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        
        builder.HasKey(oi => oi.Id);
        
        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(oi => oi.ProductSku)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(oi => oi.Quantity)
            .IsRequired();
        
        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(oi => oi.TotalPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(oi => oi.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(oi => oi.OrderId);
        
        builder.HasIndex(oi => oi.ProductId);
    }
}
