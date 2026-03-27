namespace SmartTeam.Domain.Entities;

public class QuizAnswerOption
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique short code for this answer, e.g. "ST1", "SC3", "SP2".
    /// Used as the stable identifier for admin rules.
    /// </summary>
    public string AnswerCode { get; set; } = string.Empty;

    /// <summary>
    /// The display label (Azerbaijani).
    /// </summary>
    public string AnswerText { get; set; } = string.Empty;

    /// <summary>
    /// Optional longer description shown below the label.
    /// </summary>
    public string? SubText { get; set; }

    /// <summary>
    /// Display order within the question.
    /// </summary>
    public int SortOrder { get; set; }

    public Guid QuestionId { get; set; }
    public QuizQuestion Question { get; set; } = null!;

    /// <summary>
    /// Rules that include this answer option.
    /// </summary>
    public ICollection<QuizRuleAnswer> RuleAnswers { get; set; } = new List<QuizRuleAnswer>();
}
