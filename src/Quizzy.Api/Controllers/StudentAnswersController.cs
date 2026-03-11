using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentAnswersController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all answers for a specific attempt (Teachers only)
    /// </summary>
    [HttpGet("attempt/{attemptId:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetAnswersByAttempt(Guid attemptId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var attempt = await context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(quizAttempt => quizAttempt.Student)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound(new { Message = "Attempt not found" });
        }

        // Verify teacher owns the quiz
        if (attempt.Quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var answers = await context.StudentAnswers
            .Include(a => a.Question)
                .ThenInclude(question => question.Choices)
            .Include(a => a.Choice)
            .Where(a => a.AttemptId == attemptId)
            .Select(a => a.ToStudentAnswerResultDto())
            .ToListAsync();

        return Ok(new
        {
            attemptId,
            quizId = attempt.QuizId,
            quizTitle = attempt.Quiz.Title,
            studentId = attempt.StudentId,
            studentName = attempt.Student.UserName,
            answers
        });
    }

    /// <summary>
    /// Get a specific answer by ID (Teachers only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetAnswerById(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var answer = await context.StudentAnswers
            .Include(a => a.Question)
            .ThenInclude(q => q.Choices)
            .Include(a => a.Attempt)
            .ThenInclude(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (answer == null)
        {
            return NotFound(new { Message = "Answer not found" });
        }

        // Verify teacher owns the quiz
        if (answer.Attempt.Quiz.TeacherId != userId)
        {
            return Forbid();
        }

        return Ok(answer.ToStudentAnswerResultDto());
    }

    /// <summary>
    /// Get statistics for a specific question across all attempts (Teachers only)
    /// </summary>
    [HttpGet("question/{questionId:guid}/stats")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetQuestionAnswerStats(Guid questionId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var question = await context.Questions
            .Include(q => q.Quiz)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found" });
        }

        // Verify teacher owns the quiz
        if (question.Quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var answers = await context.StudentAnswers
            .Include(a => a.Attempt)
            .ThenInclude(a => a.Student)
            .Where(a => a.QuestionId == questionId)
            .ToListAsync();

        var totalAnswers = answers.Count;
        var correctAnswers = answers.Count(a => a.IsCorrect);
        var incorrectAnswers = totalAnswers - correctAnswers;
        var accuracyRate = totalAnswers > 0
            ? (double)correctAnswers / totalAnswers * 100
            : 0;

        var choiceStats = await context.Choices
            .Where(c => c.QuestionId == questionId)
            .Select(c => new
            {
                ChoiceId = c.Id,
                Content = c.Content,
                IsCorrect = c.IsCorrect,
                SelectedCount = context.StudentAnswers.Count(a => a.QuestionId == questionId && a.ChoiceId == c.Id)
            })
            .ToListAsync();

        return Ok(new
        {
            questionId,
            questionContent = question.Content,
            totalAnswers,
            correctAnswers,
            incorrectAnswers,
            accuracyRate = Math.Round(accuracyRate, 2),
            choiceStats
        });
    }

    /// <summary>
    /// Delete an answer (Teachers only - for correction purposes)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteAnswer(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var answer = await context.StudentAnswers
            .Include(a => a.Attempt)
            .ThenInclude(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (answer == null)
        {
            return NotFound(new { Message = "Answer not found" });
        }

        // Verify teacher owns the quiz
        if (answer.Attempt.Quiz.TeacherId != userId)
        {
            return Forbid();
        }

        // Only allow deletion if attempt is not completed
        if (answer.Attempt.Status == QuizAttemptStatus.Completed)
        {
            return BadRequest(new { Message = "Cannot delete answers from completed attempts" });
        }

        context.StudentAnswers.Remove(answer);
        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Update an answer's correctness (Teachers only - for manual correction)
    /// </summary>
    [HttpPatch("{id:guid}/correct")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CorrectAnswer(Guid id, [FromBody] bool isCorrect)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var answer = await context.StudentAnswers
            .Include(a => a.Attempt)
            .ThenInclude(a => a.Quiz)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (answer == null)
        {
            return NotFound(new { Message = "Answer not found" });
        }

        // Verify teacher owns the quiz
        if (answer.Attempt.Quiz.TeacherId != userId)
        {
            return Forbid();
        }

        // Only allow correction if attempt is completed
        if (answer.Attempt.Status != QuizAttemptStatus.Completed)
        {
            return BadRequest(new { Message = "Can only correct answers from completed attempts" });
        }

        var originalIsCorrect = answer.IsCorrect;
        answer.IsCorrect = isCorrect;
        answer.UpdatedAt = DateTime.UtcNow;

        // Recalculate attempt score if correctness changed
        if (originalIsCorrect != isCorrect)
        {
            var attempt = answer.Attempt;
            var allAnswers = await context.StudentAnswers
                .Include(a => a.Question)
                .Where(a => a.AttemptId == attempt.Id)
                .ToListAsync();

            var newScore = allAnswers
                .Where(a => a.IsCorrect)
                .Sum(a => a.Question.Points);

            attempt.Score = newScore;
            attempt.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return Ok(new
        {
            answerId = answer.Id,
            isCorrect = answer.IsCorrect,
            attemptScore = answer.Attempt.Score
        });
    }
}
