using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(o => o.SubTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(o => o.DiscountAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);
        
        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(o => o.PromoCode)
            .HasMaxLength(50);
        
        builder.Property(o => o.PromoCodeDiscountPercentage)
            .HasColumnType("decimal(5,2)");
        
        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(o => o.CustomerEmail)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(o => o.CustomerPhone)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(o => o.ShippingAddress)
            .HasMaxLength(500);
        
        builder.Property(o => o.Notes)
            .HasMaxLength(1000);
        
        builder.Property(o => o.CreatedAt)
            .IsRequired();
        
        builder.Property(o => o.UpdatedAt);
        
        // Relationships
        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Relationship with Payment is configured in PaymentConfiguration
        // to avoid circular dependency and conflicting foreign key definitions.
        // builder.HasOne(o => o.Payment)
        //     .WithOne(p => p.Order)
        //     .HasForeignKey<Order>(o => o.PaymentId)
        //     .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();
        
        builder.HasIndex(o => o.UserId);
        
        builder.HasIndex(o => o.Status);
        
        builder.HasIndex(o => o.CreatedAt);
    }
}
