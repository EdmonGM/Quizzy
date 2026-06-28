using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Constants;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizAttemptsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    /// Get current user's attempts for a specific quiz (Students)
    /// </summary>
    [HttpGet("quiz/{quizId:guid}")]
    public async Task<IActionResult> GetUserAttemptsForQuiz(Guid quizId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound("Quiz not found");
        }

        var attempts = await context.QuizAttempts
            .Where(a => a.QuizId == quizId && a.StudentId == userId)
            .OrderBy(a => a.AttemptNumber)
            .Select(a => a.ToQuizAttemptSummaryDto())
            .ToListAsync();

        return Ok(attempts);
    }

    /// <summary>
    /// Get current user's in-progress attempt details with questions (Students)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAttemptById(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var attempt = await context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.StudentAnswers)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null)
        {
            return NotFound("Attempt not found");
        }

        if (attempt.StudentId != userId)
        {
            return Forbid();
        }

        if (attempt.Status != QuizAttemptStatus.InProgress)
        {
            return BadRequest("Attempt is not in progress");
        }

        var questions = await context.Questions
            .Where(q => q.QuizId == attempt.QuizId)
            .Include(q => q.Choices)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        var questionToAnswerMap = attempt.StudentAnswers
            .ToDictionary(a => a.QuestionId, a => a.ChoiceId);

        int? timeRemainingSeconds = null;
        if (attempt.Quiz.TimeLimitMinutes > 0)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
            var totalSeconds = attempt.Quiz.TimeLimitMinutes * 60;
            timeRemainingSeconds = Math.Max(0, totalSeconds - elapsedSeconds);
        }

        var response = attempt.ToQuizAttemptWithAnswersDto(questions, questionToAnswerMap, timeRemainingSeconds);

        return Ok(response);
    }

    /// <summary>
    /// Create a new quiz attempt (Students)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAttempt([FromBody] CreateQuizAttemptDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var quiz = await context.Quizzes
            .Include(q => q.Questions)
            .Include(q => q.QuizAttempts)
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound("Quiz not found");
        }

        if (!quiz.IsPublished)
        {
            return Forbid();
        }

        if (!string.IsNullOrEmpty(quiz.AccessCode))
        {
            if (string.IsNullOrEmpty(dto.AccessCode) || quiz.AccessCode != dto.AccessCode)
            {
                return Forbid();
            }
        }

        var completedAttempts = quiz.QuizAttempts
            .Count(a => a.StudentId == userId && a.Status == QuizAttemptStatus.Completed);

        if (quiz.MaxAttempts.HasValue && completedAttempts >= quiz.MaxAttempts.Value)
        {
            return BadRequest("Maximum number of attempts reached");
        }

        var existingInProgress = await context.QuizAttempts
            .FirstOrDefaultAsync(a => a.QuizId == dto.QuizId && a.StudentId == userId && a.Status == QuizAttemptStatus.InProgress);

        if (existingInProgress != null)
        {
            return Conflict("You already have an in-progress attempt for this quiz");
        }

        var attemptNumber = quiz.QuizAttempts.Count(a => a.StudentId == userId) + 1;

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            StudentId = userId,
            AttemptNumber = attemptNumber,
            Status = QuizAttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            Score = 0,
            TotalPossibleScore = quiz.Questions.Sum(q => q.Points),
            TimeSpentSeconds = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.QuizAttempts.AddAsync(attempt);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAttemptById), new { id = attempt.Id }, attempt.ToCreateAttemptResponseDto());
    }

    /// <summary>
    /// Submit an answer for a question in the attempt (Students)
    /// </summary>
    [HttpPost("{attemptId:guid}/answers")]
    public async Task<IActionResult> SubmitAnswer(Guid attemptId, [FromBody] SubmitAnswerDto dto)
    {
        var currentUserId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized("User not authenticated");
        }

        var attempt = await context.QuizAttempts
            .Include(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound("Attempt not found");
        }

        if (attempt.StudentId != currentUserId)
        {
            return Forbid();
        }

        if (attempt.Status != QuizAttemptStatus.InProgress)
        {
            return BadRequest("Attempt is not in progress");
        }

        if (attempt.Quiz.TimeLimitMinutes > 0)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
            var totalSeconds = attempt.Quiz.TimeLimitMinutes * 60;
            if (elapsedSeconds >= totalSeconds)
            {
                return BadRequest("Time limit exceeded");
            }
        }

        var question = await context.Questions
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == dto.QuestionId && q.QuizId == attempt.QuizId);

        if (question == null)
        {
            return NotFound("Question not found in this quiz");
        }

        var choice = await context.Choices
            .FirstOrDefaultAsync(c => c.Id == dto.ChoiceId && c.QuestionId == dto.QuestionId);

        if (choice == null)
        {
            return NotFound("Choice not found for this question");
        }

        var existingAnswer = await context.StudentAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == dto.QuestionId);

        if (existingAnswer != null)
        {
            existingAnswer.ChoiceId = dto.ChoiceId;
            existingAnswer.ChoiceSnapshot = choice.Content;
            existingAnswer.QuestionSnapshot = question.Content;
            existingAnswer.IsCorrect = choice.IsCorrect;
            existingAnswer.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return Ok(new SubmitAnswerResponseDto
            {
                AnswerId = existingAnswer.Id,
                Saved = true
            });
        }

        var answer = new StudentAnswer
        {
            Id = Guid.NewGuid(),
            AttemptId = attemptId,
            QuestionId = dto.QuestionId,
            ChoiceId = dto.ChoiceId,
            QuestionSnapshot = question.Content,
            ChoiceSnapshot = choice.Content,
            IsCorrect = choice.IsCorrect,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.StudentAnswers.AddAsync(answer);
        await context.SaveChangesAsync();

        return Ok(new SubmitAnswerResponseDto
        {
            AnswerId = answer.Id,
            Saved = true
        });
    }

    /// <summary>
    /// Submit and complete the attempt (Students)
    /// </summary>
    [HttpPost("{attemptId:guid}/submit")]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId)
    {
        var currentUserId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized("User not authenticated");
        }

        var attempt = await context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.StudentAnswers)
            .ThenInclude(sa => sa.Question)
            .ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound("Attempt not found");
        }

        if (attempt.StudentId != currentUserId)
        {
            return Forbid();
        }

        if (attempt.Status != QuizAttemptStatus.InProgress)
        {
            return BadRequest("Attempt is not in progress");
        }

        if (attempt.Quiz.TimeLimitMinutes > 0)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
            var totalSeconds = attempt.Quiz.TimeLimitMinutes * 60;
            if (elapsedSeconds >= totalSeconds)
            {
                attempt.Status = QuizAttemptStatus.Completed;
                attempt.CompletedAt = DateTime.UtcNow;
            }

            attempt.TimeSpentSeconds = elapsedSeconds;
        }
        else
        {
            attempt.TimeSpentSeconds = (int)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
        }

        attempt.Status = QuizAttemptStatus.Completed;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.UpdatedAt = DateTime.UtcNow;

        var score = 0;
        foreach (var answer in attempt.StudentAnswers)
        {
            if (!answer.IsCorrect) continue;
            score += answer.Question.Points;
        }

        attempt.Score = score;

        await context.SaveChangesAsync();

        var answers = attempt.StudentAnswers.ToList();
        return Ok(attempt.ToSubmitAttemptResponseDto(answers));
    }

    /// <summary>
    /// Abandon an in-progress attempt (Students)
    /// </summary>
    [HttpPost("{attemptId:guid}/abandon")]
    public async Task<IActionResult> AbandonAttempt(Guid attemptId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var attempt = await context.QuizAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound("Attempt not found");
        }

        if (attempt.StudentId != userId)
        {
            return Forbid();
        }

        if (attempt.Status != QuizAttemptStatus.InProgress)
        {
            return BadRequest("Attempt is not in progress");
        }

        attempt.Status = QuizAttemptStatus.Abandoned;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(attempt.ToAbandonAttemptResponseDto());
    }

    /// <summary>
    /// Get attempt results after completion (Students)
    /// </summary>
    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> GetAttemptResults(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var attempt = await context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.StudentAnswers)
            .ThenInclude(sa => sa.Question)
            .ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null)
        {
            return NotFound("Attempt not found");
        }

        if (attempt.StudentId != userId)
        {
            return Forbid();
        }

        if (attempt.Status != QuizAttemptStatus.Completed)
        {
            return BadRequest("Attempt is not completed");
        }

        var answers = attempt.StudentAnswers.ToList();
        return Ok(attempt.ToQuizAttemptResultsDto(answers));
    }

    /// <summary>
    /// Get overview of all attempts for a quiz (Teachers)
    /// </summary>
    [HttpGet("overview/quiz/{quizId:guid}")]
    [Authorize(Roles = AppRoles.Teacher)]
    public async Task<IActionResult> GetQuizAttemptsOverview(Guid quizId, [FromQuery] string? status)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound("Quiz not found");
        }

        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var query = context.QuizAttempts
            .Where(a => a.QuizId == quizId)
            .Include(a => a.Student)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Status == status);
        }

        var attempts = await query.ToListAsync();

        return Ok(attempts.ToQuizAttemptsOverviewDto(quiz.Title, quiz.Id));
    }

    /// <summary>
    /// Get detailed attempt information (Teachers)
    /// </summary>
    [HttpGet("{id:guid}/details")]
    [Authorize(Roles = AppRoles.Teacher)]
    public async Task<IActionResult> GetAttemptDetails(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User not authenticated");
        }

        var attempt = await context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Student)
            .Include(a => a.StudentAnswers)
            .ThenInclude(sa => sa.Question)
            .ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attempt == null)
        {
            return NotFound("Attempt not found");
        }

        if (attempt.Quiz.TeacherId != userId)
        {
            return Forbid();
        }

        var answers = attempt.StudentAnswers.ToList();
        return Ok(attempt.ToQuizAttemptResultsDto(answers));
    }
}
