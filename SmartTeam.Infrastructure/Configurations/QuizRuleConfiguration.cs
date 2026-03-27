using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class QuizRuleConfiguration : IEntityTypeConfiguration<QuizRule>
{
    public void Configure(EntityTypeBuilder<QuizRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RuleDescription)
            .HasMaxLength(500);

        builder.HasMany(r => r.Answers)
            .WithOne(ra => ra.Rule)
            .HasForeignKey(ra => ra.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Products)
            .WithOne(rp => rp.Rule)
            .HasForeignKey(rp => rp.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
