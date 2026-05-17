using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

/// <summary>
/// Manages question operations including listing, retrieving, creating, updating, deleting, and reordering questions.
/// </summary>
/// <remarks>
/// This controller requires authentication for all endpoints. Teacher role is required for create, update, delete, and reorder operations.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const string RoleTeacher = "Teacher";
    private const string UserNotAuthenticatedMessage = "User not authenticated";
    private const string QuestionNotFoundMessage = "Question not found";
    private const string QuizNotFoundMessage = "Quiz not found";
    private const string CannotDeleteQuestionWithAnswersMessage = "Cannot delete question with existing student answers";
    private const string QuestionsNotFoundMessage = "One or more questions not found";
    private const string AtLeastOneCorrectChoiceMessage = "At least one choice must be marked as correct";

    /// <summary>
    /// Retrieves all questions for a specific quiz, ordered by their index.
    /// </summary>
    /// <param name="quizId">The GUID of the quiz whose questions to retrieve.</param>
    /// <returns>List of questions with their choices.</returns>
    /// <response code="200">Returns the list of questions.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the quiz is not found.</response>
    [HttpGet("quiz/{quizId:guid}")]
    [ProducesResponseType(typeof(List<QuestionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionsByQuizId(Guid quizId)
    {
        var quizExists = await context.Quizzes
            .AnyAsync(q => q.Id == quizId && !q.IsDeleted);

        if (!quizExists)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

        var questions = await context.Questions
            .Include(q => q.Choices)
            .Where(q => q.QuizId == quizId)
            .OrderBy(q => q.OrderIndex)
            .Select(q => q.ToQuestionResponseDto())
            .ToListAsync();

        return Ok(questions);
    }

    /// <summary>
    /// Retrieves a question by its unique identifier, including its choices.
    /// </summary>
    /// <param name="id">The GUID of the question to retrieve.</param>
    /// <returns>The question details if found.</returns>
    /// <response code="200">Returns the requested question.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the question is not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionById(Guid id)
    {
        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { message = QuestionNotFoundMessage });
        }

        return Ok(question.ToQuestionResponseDto());
    }

    /// <summary>
    /// Creates a new question with choices for a quiz.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. The user must own the quiz.</remarks>
    /// <param name="dto">The question and choices data to create.</param>
    /// <returns>The created question details.</returns>
    /// <response code="201">Returns the newly created question.</response>
    /// <response code="400">If the request body is invalid or no choice is marked as correct.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the quiz.</response>
    /// <response code="404">If the quiz is not found.</response>
    [HttpPost]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!dto.Choices.Any(c => c.IsCorrect))
        {
            return BadRequest(new { message = AtLeastOneCorrectChoiceMessage });
        }

        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var question = dto.ToQuestion();
        context.Questions.Add(question);

        foreach (var choiceDto in dto.Choices)
        {
            var choice = choiceDto.ToChoice(question.Id);
            context.Choices.Add(choice);
        }

        await context.SaveChangesAsync();

        var createdQuestion = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == question.Id);

        return CreatedAtAction(nameof(GetQuestionById), new { id = question.Id }, createdQuestion!.ToQuestionResponseDto());
    }

    /// <summary>
    /// Updates an existing question and its choices.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. The user must own the quiz this question belongs to.</remarks>
    /// <param name="id">The GUID of the question to update.</param>
    /// <param name="dto">The updated question and choices data.</param>
    /// <returns>The updated question details.</returns>
    /// <response code="200">Returns the updated question.</response>
    /// <response code="400">If the request body is invalid or no choice is marked as correct.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the quiz.</response>
    /// <response code="404">If the question or quiz is not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!dto.Choices.Any(c => c.IsCorrect))
        {
            return BadRequest(new { message = AtLeastOneCorrectChoiceMessage });
        }

        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { message = QuestionNotFoundMessage });
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == question.QuizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        question.UpdateQuestionFromDto(dto);

        var existingChoiceIds = question.Choices.Select(c => c.Id).ToList();
        var incomingChoiceIds = dto.Choices.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToList();

        var choicesToDelete = question.Choices.Where(c => !incomingChoiceIds.Contains(c.Id)).ToList();
        foreach (var choice in choicesToDelete)
        {
            context.Choices.Remove(choice);
        }

        foreach (var choiceDto in dto.Choices)
        {
            if (choiceDto.Id.HasValue && existingChoiceIds.Contains(choiceDto.Id.Value))
            {
                var existingChoice = question.Choices.First(c => c.Id == choiceDto.Id.Value);
                existingChoice.UpdateChoiceFromDto(choiceDto);
            }
            else
            {
                var newChoice = choiceDto.ToChoice(question.Id);
                question.Choices.Add(newChoice);
            }
        }

        await context.SaveChangesAsync();

        return Ok(question.ToQuestionResponseDto());
    }

    /// <summary>
    /// Deletes a question and its associated choices.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. The user must own the quiz. Cannot delete questions with existing student answers.</remarks>
    /// <param name="id">The GUID of the question to delete.</param>
    /// <response code="204">Question deleted successfully.</response>
    /// <response code="400">If the question has existing student answers.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the quiz.</response>
    /// <response code="404">If the question or quiz is not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { message = QuestionNotFoundMessage });
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == question.QuizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var hasAnswers = await context.StudentAnswers
            .AnyAsync(a => a.QuestionId == id);

        if (hasAnswers)
        {
            return BadRequest(new { message = CannotDeleteQuestionWithAnswersMessage });
        }

        context.Choices.RemoveRange(question.Choices);
        context.Questions.Remove(question);

        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Reorders questions within a quiz.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. The user must own all questions being reordered.</remarks>
    /// <param name="dto">List of question IDs and their new order indices.</param>
    /// <response code="204">Questions reordered successfully.</response>
    /// <response code="400">If the request body is invalid.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the questions.</response>
    /// <response code="404">If one or more questions are not found.</response>
    [HttpPut("reorder")]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderQuestions([FromBody] List<ReorderQuestionDto> dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var questionIds = dto.Select(d => d.QuestionId).ToList();
        var questions = await context.Questions
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var foundIds = questions.Select(q => q.Id).ToHashSet();
        var missingIds = dto.Select(d => d.QuestionId).Where(id => !foundIds.Contains(id)).ToList();

        if (missingIds.Any())
        {
            return NotFound(new { message = QuestionsNotFoundMessage });
        }

        var quizIds = questions.Select(q => q.QuizId).Distinct().ToList();
        var quizzes = await context.Quizzes
            .Where(q => quizIds.Contains(q.Id) && !q.IsDeleted)
            .ToListAsync();

        if (quizzes.Any(q => q.TeacherId != userId))
        {
            return Forbid();
        }

        foreach (var item in dto)
        {
            var question = questions.First(q => q.Id == item.QuestionId);
            question.OrderIndex = item.OrderIndex;
            question.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return NoContent();
    }
}
