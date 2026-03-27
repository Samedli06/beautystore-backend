using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class QuizAnswerOptionConfiguration : IEntityTypeConfiguration<QuizAnswerOption>
{
    public void Configure(EntityTypeBuilder<QuizAnswerOption> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AnswerCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.AnswerText)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.SubText)
            .HasMaxLength(500);

        builder.HasIndex(a => a.AnswerCode)
            .IsUnique();
    }
}
