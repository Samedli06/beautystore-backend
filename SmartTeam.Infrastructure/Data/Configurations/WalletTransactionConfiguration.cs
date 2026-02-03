using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Amount)
               .HasColumnType("decimal(18,2)");
               
        builder.Property(t => t.Description)
               .HasMaxLength(500);

        builder.HasOne(t => t.Order)
               .WithMany()
               .HasForeignKey(t => t.OrderId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
