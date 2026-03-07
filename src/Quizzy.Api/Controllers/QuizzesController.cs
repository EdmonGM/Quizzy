using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all published, not deleted quizzes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllQuizzes()
    {
        var quizzes = await context.Quizzes
            .Include(q => q.Category)
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .Where(q => !q.IsDeleted && q.IsPublished)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => q.ToQuizResponseDto())
            .ToListAsync();

        return Ok(quizzes);
    }
    
    /// <summary>   
    /// Get all quizzes by the current authenticated user (including drafts)
    /// </summary>
    [HttpGet("my-quizzes")]
    public async Task<IActionResult> GetMyQuizzes()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var quizzes = await context.Quizzes
            .Include(q => q.Category)
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .Where(q => !q.IsDeleted && q.TeacherId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => q.ToQuizResponseDto())
            .ToListAsync();

        return Ok(quizzes);
    }

    /// <summary>
    /// Get all quizzes by a specific teacher (including drafts)
    /// </summary>
    [HttpGet("teacher/{teacherId}")]
    public async Task<IActionResult> GetQuizzesByTeacher(string teacherId)
    {
        var quizzes = await context.Quizzes
            .Include(q => q.Category)
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .Where(q => !q.IsDeleted && q.TeacherId == teacherId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => q.ToQuizResponseDto())
            .ToListAsync();

        return Ok(quizzes);
    }

    /// <summary>
    /// Get all quizzes by category
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    public async Task<IActionResult> GetQuizzesByCategory(Guid categoryId)
    {
        var quizzes = await context.Quizzes
            .Include(q => q.Category)
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .Where(q => !q.IsDeleted && q.IsPublished && q.CategoryId == categoryId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => q.ToQuizResponseDto())
            .ToListAsync();

        return Ok(quizzes);
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
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
                .ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound(new { Message = "Quiz not found" });
        }

        return Ok(quiz.ToQuizDetailedResponseDto());
    }
    
    /// <summary>
    /// Create a new quiz
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

        var quiz = dto.ToQuiz(userId);

        await context.Quizzes.AddAsync(quiz);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, quiz.ToQuizResponseDto());
    }

    /// <summary>
    /// Update a quiz
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
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
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

        return Ok(quiz.ToQuizResponseDto());
    }

    /// <summary>
    /// Delete a quiz (soft delete)
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

        quiz.IsDeleted = true;
        quiz.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Publish/Unpublish a quiz
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

        return Ok(quiz.ToQuizResponseDto());
    }
}

