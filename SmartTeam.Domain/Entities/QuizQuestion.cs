namespace SmartTeam.Domain.Entities;

public class QuizQuestion
{
    public Guid Id { get; set; }

    /// <summary>
    /// The question text in Azerbaijani.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Logical step key: "SkinType" | "SkinConcern" | "SpfPreference"
    /// </summary>
    public string StepKey { get; set; } = string.Empty;

    /// <summary>
    /// Display order (1, 2, 3).
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<QuizAnswerOption> AnswerOptions { get; set; } = new List<QuizAnswerOption>();
}
