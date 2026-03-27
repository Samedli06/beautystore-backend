using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    /// <summary>
    /// Get all quiz questions with their answer options (Azerbaijani).
    /// Returns questions ordered by step: Skin Type → Skin Concern → SPF Preference.
    /// </summary>
    [HttpGet("questions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<QuizQuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuizQuestionDto>>> GetQuestions(CancellationToken cancellationToken)
    {
        var questions = await _quizService.GetAllQuestionsAsync(cancellationToken);
        return Ok(questions);
    }

    /// <summary>
    /// Submit quiz answers and receive product recommendations.
    /// The user must select exactly one answer per question.
    /// Products are only returned after all 3 questions are answered.
    /// </summary>
    [HttpPost("submit")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(QuizResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuizResultDto>> SubmitQuiz(
        [FromBody] QuizSubmitDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _quizService.SubmitQuizAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
