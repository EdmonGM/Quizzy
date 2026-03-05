using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizzesController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all published, not deleted quizzes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllQuizzes()
    {
        var quizzes = await context.Quizzes
            .Where(q => !q.IsDeleted && q.IsPublished)
            .Include(q => q.Category)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var result = new List<QuizResponseDto>();
        foreach (var quiz in quizzes)
        {
            result.Add(await quiz.ToQuizResponseDtoAsync(context));
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all quizzes by the current authenticated user (including drafts)
    /// </summary>
    [HttpGet("my-quizzes")]
    [Authorize]
    public async Task<IActionResult> GetMyQuizzes()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var quizzes = await context.Quizzes
            .Where(q => !q.IsDeleted && q.TeacherId == userId)
            .Include(q => q.Category)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var result = new List<QuizResponseDto>();
        foreach (var quiz in quizzes)
        {
            result.Add(await quiz.ToQuizResponseDtoAsync(context));
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all quizzes by a specific teacher (including drafts)
    /// </summary>
    [HttpGet("teacher/{teacherId}")]
    public async Task<IActionResult> GetQuizzesByTeacher(string teacherId)
    {
        var quizzes = await context.Quizzes
            .Where(q => !q.IsDeleted && q.TeacherId == teacherId)
            .Include(q => q.Category)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var result = new List<QuizResponseDto>();
        foreach (var quiz in quizzes)
        {
            result.Add(await quiz.ToQuizResponseDtoAsync(context));
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all quizzes by category
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    public async Task<IActionResult> GetQuizzesByCategory(Guid categoryId)
    {
        var quizzes = await context.Quizzes
            .Where(q => !q.IsDeleted && q.IsPublished && q.CategoryId == categoryId)
            .Include(q => q.Category)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var result = new List<QuizResponseDto>();
        foreach (var quiz in quizzes)
        {
            result.Add(await quiz.ToQuizResponseDtoAsync(context));
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a quiz by ID (detail view with questions)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetQuizById(Guid id)
    {
        var quiz = await context.Quizzes
            .Where(q => !q.IsDeleted)
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound(new { Message = "Quiz not found" });
        }

        return Ok(await quiz.ToQuizDetailResponseDtoAsync(context));
    }

    /// <summary>
    /// Create a new quiz (requires authentication)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get current user ID from JWT token
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        // Verify category exists
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);

        if (category == null)
        {
            return BadRequest(new { Message = "Invalid category" });
        }

        var quiz = dto.ToQuiz();
        quiz.TeacherId = userId;

        await context.Quizzes.AddAsync(quiz);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, await quiz.ToQuizResponseDtoAsync(context));
    }

    /// <summary>
    /// Update a quiz (requires authentication)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> UpdateQuiz(Guid id, [FromBody] UpdateQuizDto dto)
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

        var quiz = await context.Quizzes
            .Where(q => !q.IsDeleted)
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound(new { Message = "Quiz not found" });
        }

        // Check if user owns this quiz
        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        // Verify new category exists
        if (quiz.CategoryId != dto.CategoryId)
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);

            if (category == null)
            {
                return BadRequest(new { Message = "Invalid category" });
            }
        }

        quiz.UpdateQuizFromDto(dto);
        await context.SaveChangesAsync();

        return Ok(await quiz.ToQuizResponseDtoAsync(context));
    }

    /// <summary>
    /// Delete a quiz (soft delete) (requires authentication)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize (Roles = "Teacher")]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null || quiz.IsDeleted)
        {
            return NotFound(new { Message = "Quiz not found" });
        }

        // Check if user owns this quiz
        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        // Check if quiz has attempts
        var hasAttempts = await context.QuizAttempts
            .AnyAsync(a => a.QuizId == id);

        if (hasAttempts)
        {
            return BadRequest(new { Message = "Cannot delete quiz with existing attempts" });
        }

        quiz.IsDeleted = true;
        quiz.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Publish/Unpublish a quiz (requires authentication)
    /// </summary>
    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> TogglePublish(Guid id, [FromBody] bool isPublished)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null || quiz.IsDeleted)
        {
            return NotFound(new { Message = "Quiz not found" });
        }

        // Check if user owns this quiz
        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        quiz.IsPublished = isPublished;
        quiz.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(await quiz.ToQuizResponseDtoAsync(context));
    }
}

