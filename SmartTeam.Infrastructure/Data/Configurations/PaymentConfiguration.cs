using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.EpointTransactionId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("AZN");
        
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Epoint");
        
        builder.Property(p => p.RequestData)
            .HasColumnType("nvarchar(max)");
        
        builder.Property(p => p.ResponseData)
            .HasColumnType("nvarchar(max)");
        
        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(1000);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        builder.Property(p => p.CompletedAt);
        
        
        // Relationships
        builder.HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        
        builder.HasOne(p => p.PendingOrder)
            .WithMany()
            .HasForeignKey(p => p.PendingOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(p => p.EpointTransactionId)
            .IsUnique();
        
        builder.HasIndex(p => p.OrderId);
        
        builder.HasIndex(p => p.PendingOrderId);
        
        builder.HasIndex(p => p.Status);
        
        builder.HasIndex(p => p.CreatedAt);
    }
}
