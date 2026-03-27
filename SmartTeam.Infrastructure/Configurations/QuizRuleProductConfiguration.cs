using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class QuizRuleProductConfiguration : IEntityTypeConfiguration<QuizRuleProduct>
{
    public void Configure(EntityTypeBuilder<QuizRuleProduct> builder)
    {
        // Composite primary key
        builder.HasKey(rp => new { rp.RuleId, rp.ProductId });

        builder.HasOne(rp => rp.Rule)
            .WithMany(r => r.Products)
            .HasForeignKey(rp => rp.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Product)
            .WithMany()
            .HasForeignKey(rp => rp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
