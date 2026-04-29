using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class CreditRequestConfiguration : IEntityTypeConfiguration<CreditRequest>
{
    public void Configure(EntityTypeBuilder<CreditRequest> builder)
    {
        builder.ToTable("CreditRequests");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cr => cr.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cr => cr.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(cr => cr.Status)
            .HasDefaultValue(CreditRequestStatus.Pending);

        // UserId is required — user must be authenticated when submitting
        builder.Property(cr => cr.UserId)
            .IsRequired();

        // ConvertedOrderId links back to the Order created upon Approval (nullable)
        builder.Property(cr => cr.ConvertedOrderId)
            .IsRequired(false);

        builder.Property(cr => cr.Notes)
            .HasMaxLength(2000);

        builder.Property(cr => cr.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // One credit request has many items; delete items when request is deleted
        builder.HasMany(cr => cr.Items)
            .WithOne(i => i.CreditRequest)
            .HasForeignKey(i => i.CreditRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CreditRequestItemConfiguration : IEntityTypeConfiguration<CreditRequestItem>
{
    public void Configure(EntityTypeBuilder<CreditRequestItem> builder)
    {
        builder.ToTable("CreditRequestItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.ProductSku)
            .HasMaxLength(100);

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(i => i.TotalPrice)
            .HasPrecision(18, 2);
    }
}
