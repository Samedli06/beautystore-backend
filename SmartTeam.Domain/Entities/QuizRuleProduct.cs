namespace SmartTeam.Domain.Entities;

/// <summary>
/// Join table between QuizRule and Product.
/// Defines which products are recommended when a rule matches.
/// </summary>
public class QuizRuleProduct
{
    public Guid RuleId { get; set; }
    public QuizRule Rule { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
