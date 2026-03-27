using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IQuizService
{
    // ─── Public ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all quiz questions with their answer options, ordered by SortOrder.
    /// </summary>
    Task<IEnumerable<QuizQuestionDto>> GetAllQuestionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the user answered all required questions and returns
    /// recommended products based on admin-defined rules.
    /// </summary>
    Task<QuizResultDto> SubmitQuizAsync(QuizSubmitDto dto, CancellationToken cancellationToken = default);

    // ─── Admin ─────────────────────────────────────────────────────────────

    Task<IEnumerable<QuizRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default);
    Task<QuizRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuizRuleDto> CreateRuleAsync(CreateQuizRuleDto dto, CancellationToken cancellationToken = default);
    Task<QuizRuleDto> UpdateRuleAsync(Guid id, UpdateQuizRuleDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);
}
