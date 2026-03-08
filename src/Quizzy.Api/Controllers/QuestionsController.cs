using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class QuestionsController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all questions for a specific quiz
    /// </summary>
    [HttpGet("quiz/{quizId:guid}")]
    public async Task<IActionResult> GetQuestionsByQuiz(Guid quizId)
    {
        var questions = await context.Questions
            .Include(q => q.Choices)
            .Where(q => q.QuizId == quizId)
            .OrderBy(q => q.OrderIndex)
            .Select(q => q.ToQuestionResponseDto())
            .ToListAsync();

        return Ok(questions);
    }

    /// <summary>
    /// Get a question by ID with its choices
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetQuestionById(Guid id)
    {
        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found" });
        }

        return Ok(question.ToQuestionResponseDto());
    }

    /// <summary>
    /// Create a new question with choices for a quiz
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        // Verify quiz exists and user owns it
        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound(new { Message = "Quiz not found" });
        }

        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var question = dto.ToQuestion();
        await context.Questions.AddAsync(question);

        // Add choices
        foreach (var choiceDto in dto.Choices)
        {
            var choice = choiceDto.ToChoice(question.Id);
            await context.Choices.AddAsync(choice);
        }

        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestionById), new { id = question.Id }, question.ToQuestionResponseDto());
    }

    /// <summary>
    /// Update a question and its choices
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found" });
        }

        // Verify user owns the quiz this question belongs to
        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == question.QuizId && !q.IsDeleted);

        if (quiz == null || quiz.TeacherId != userId)
        {
            return Forbid();
        }

        question.UpdateQuestionFromDto(dto);

        // Update existing choices and add new ones
        var existingChoiceIds = question.Choices.Select(c => c.Id).ToList();
        var incomingChoiceIds = dto.Choices.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToList();

        // Remove choices that are no longer present
        var choicesToDelete = question.Choices.Where(c => !incomingChoiceIds.Contains(c.Id)).ToList();
        foreach (var choice in choicesToDelete)
        {
            context.Choices.Remove(choice);
        }

        // Update or add choices
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
                await context.Choices.AddAsync(newChoice);
            }
        }

        await context.SaveChangesAsync();

        return Ok(question.ToQuestionResponseDto());
    }

    /// <summary>
    /// Delete a question (and its choices)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found" });
        }

        // Verify user owns the quiz this question belongs to
        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == question.QuizId && !q.IsDeleted);

        if (quiz == null || quiz.TeacherId != userId)
        {
            return Forbid();
        }

        // Check if question has student answers
        var hasAnswers = await context.StudentAnswers
            .AnyAsync(a => a.QuestionId == id);

        if (hasAnswers)
        {
            return BadRequest(new { Message = "Cannot delete question with existing student answers" });
        }

        // Remove associated choices
        context.Choices.RemoveRange(question.Choices);
        context.Questions.Remove(question);

        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Reorder questions in a quiz
    /// </summary>
    [HttpPut("reorder")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> ReorderQuestions([FromBody] List<ReorderQuestionDto> dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var questionIds = dto.Select(d => d.QuestionId).ToList();
        var questions = await context.Questions
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        // Verify user owns all questions
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

