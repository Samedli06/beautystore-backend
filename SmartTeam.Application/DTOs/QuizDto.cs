namespace SmartTeam.Application.DTOs;

// ─── Public-facing DTOs (questions + submit) ───────────────────────────────

public class QuizAnswerOptionDto
{
    public Guid Id { get; set; }
    public string AnswerCode { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public string? SubText { get; set; }
    public int SortOrder { get; set; }
}

public class QuizQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string StepKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<QuizAnswerOptionDto> AnswerOptions { get; set; } = new();
}

/// <summary>
/// Sent by the user when submitting quiz answers.
/// Must include exactly one answer per question.
/// </summary>
public class QuizSubmitDto
{
    /// <summary>
    /// IDs of the selected QuizAnswerOptions (one per question).
    /// </summary>
    public List<Guid> SelectedAnswerIds { get; set; } = new();
}

/// <summary>
/// Returned to the user after submitting the quiz.
/// </summary>
public class QuizResultDto
{
    public List<ProductDto> RecommendedProducts { get; set; } = new();
}

// ─── Admin Rule DTOs ────────────────────────────────────────────────────────

public class QuizRuleAnswerDto
{
    public Guid AnswerOptionId { get; set; }
    public string AnswerCode { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
}

public class QuizRuleProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class QuizRuleDto
{
    public Guid Id { get; set; }
    public string? RuleDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<QuizRuleAnswerDto> Answers { get; set; } = new();
    public List<QuizRuleProductDto> Products { get; set; } = new();
}

public class CreateQuizRuleDto
{
    /// <summary>
    /// Optional admin-facing label for this rule.
    /// </summary>
    public string? RuleDescription { get; set; }

    /// <summary>
    /// All answer option IDs that must be selected to trigger this rule.
    /// </summary>
    public List<Guid> AnswerOptionIds { get; set; } = new();

    /// <summary>
    /// Product IDs to recommend when this rule matches.
    /// </summary>
    public List<Guid> ProductIds { get; set; } = new();
}

public class UpdateQuizRuleDto
{
    public string? RuleDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> AnswerOptionIds { get; set; } = new();
    public List<Guid> ProductIds { get; set; } = new();
}
