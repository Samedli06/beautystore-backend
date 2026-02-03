using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.Balance)
               .HasColumnType("decimal(18,2)")
               .HasDefaultValue(0);
        
        builder.HasOne(w => w.User)
               .WithOne(u => u.Wallet)
               .HasForeignKey<Wallet>(w => w.UserId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasMany(w => w.Transactions)
               .WithOne(t => t.Wallet)
               .HasForeignKey(t => t.WalletId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
