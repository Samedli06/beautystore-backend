using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class QuizRuleAnswerConfiguration : IEntityTypeConfiguration<QuizRuleAnswer>
{
    public void Configure(EntityTypeBuilder<QuizRuleAnswer> builder)
    {
        // Composite primary key
        builder.HasKey(ra => new { ra.RuleId, ra.AnswerOptionId });

        builder.HasOne(ra => ra.Rule)
            .WithMany(r => r.Answers)
            .HasForeignKey(ra => ra.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.AnswerOption)
            .WithMany(a => a.RuleAnswers)
            .HasForeignKey(ra => ra.AnswerOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
