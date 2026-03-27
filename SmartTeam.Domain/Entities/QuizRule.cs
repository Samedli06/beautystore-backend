namespace SmartTeam.Domain.Entities;

/// <summary>
/// Admin-defined rule: a set of answer options that, when all selected by the user, 
/// trigger the recommendation of a set of products.
/// </summary>
public class QuizRule
{
    public Guid Id { get; set; }

    /// <summary>
    /// Optional admin-facing label, e.g. "Quru dəri + Qırışlar → Antiaging krem".
    /// </summary>
    public string? RuleDescription { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Answer options that ALL must be selected to trigger this rule.
    /// </summary>
    public ICollection<QuizRuleAnswer> Answers { get; set; } = new List<QuizRuleAnswer>();

    /// <summary>
    /// Products to recommend when this rule matches.
    /// </summary>
    public ICollection<QuizRuleProduct> Products { get; set; } = new List<QuizRuleProduct>();
}
