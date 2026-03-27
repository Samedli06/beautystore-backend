namespace SmartTeam.Domain.Entities;

/// <summary>
/// Join table between QuizRule and QuizAnswerOption.
/// Defines which answer options belong to a rule.
/// </summary>
public class QuizRuleAnswer
{
    public Guid RuleId { get; set; }
    public QuizRule Rule { get; set; } = null!;

    public Guid AnswerOptionId { get; set; }
    public QuizAnswerOption AnswerOption { get; set; } = null!;
}
