using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;

namespace Quizzy.Api.Controllers;

/// <summary>
/// Manages quiz operations including listing, retrieving, creating, updating, deleting, and publishing quizzes.
/// </summary>
/// <remarks>
/// This controller requires authentication for all endpoints. Teacher role is required for create, update, delete, and publish operations.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController(ApplicationDbContext context) : ControllerBase
{
    private const string RoleTeacher = "Teacher";
    private const string QuizNotFoundMessage = "Quiz not found";
    private const string UserNotAuthenticatedMessage = "User not authenticated";
    private const string InvalidCategoryMessage = "Invalid category";

    /// <summary>
    /// Retrieves all active, published quizzes ordered by creation date (newest first).
    /// </summary>
    /// <returns>List of published quizzes.</returns>
    /// <response code="200">Returns the list of quizzes.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<QuizResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Retrieves all quizzes (including drafts) for the currently authenticated user.
    /// </summary>
    /// <returns>List of quizzes owned by the current user.</returns>
    /// <response code="200">Returns the list of quizzes.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet("my-quizzes")]
    [ProducesResponseType(typeof(List<QuizResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyQuizzes()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
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
    /// Retrieves all quizzes (including drafts) for a specific teacher.
    /// </summary>
    /// <param name="teacherId">The ID of the teacher whose quizzes to retrieve.</param>
    /// <returns>List of quizzes owned by the specified teacher.</returns>
    /// <response code="200">Returns the list of quizzes.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet("teacher/{teacherId}")]
    [ProducesResponseType(typeof(List<QuizResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Retrieves all published quizzes for a specific category.
    /// </summary>
    /// <param name="categoryId">The GUID of the category to filter quizzes by.</param>
    /// <returns>List of published quizzes in the specified category.</returns>
    /// <response code="200">Returns the list of quizzes.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet("category/{categoryId:guid}")]
    [ProducesResponseType(typeof(List<QuizResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Retrieves a quiz by its unique identifier, including all questions and their choices.
    /// </summary>
    /// <param name="id">The GUID of the quiz to retrieve.</param>
    /// <returns>The quiz details if found.</returns>
    /// <response code="200">Returns the requested quiz.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the quiz is not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuizDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return NotFound(new { message = QuizNotFoundMessage });
        }

        return Ok(quiz.ToQuizDetailedResponseDto());
    }

    /// <summary>
    /// Creates a new quiz.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role.</remarks>
    /// <param name="dto">The quiz data to create.</param>
    /// <returns>The created quiz details.</returns>
    /// <response code="201">Returns the newly created quiz.</response>
    /// <response code="400">If the request body is invalid or the category does not exist.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role.</response>
    [HttpPost]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);

        if (category == null)
        {
            return BadRequest(new { message = InvalidCategoryMessage });
        }

        var quiz = dto.ToQuiz(userId);

        context.Quizzes.Add(quiz);
        await context.SaveChangesAsync();

        var createdQuiz = await context.Quizzes
            .Include(q => q.Teacher)
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == quiz.Id);

        return CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, createdQuiz!.ToQuizResponseDto());
    }

    /// <summary>
    /// Updates an existing quiz.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. Only the quiz owner can update it.</remarks>
    /// <param name="id">The GUID of the quiz to update.</param>
    /// <param name="dto">The updated quiz data.</param>
    /// <returns>The updated quiz details.</returns>
    /// <response code="200">Returns the updated quiz.</response>
    /// <response code="400">If the request body is invalid or the category does not exist.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the quiz.</response>
    /// <response code="404">If the quiz is not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuiz(Guid id, [FromBody] UpdateQuizDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var quiz = await context.Quizzes
            .Where(q => !q.IsDeleted)
            .Include(q => q.Category)
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

        if (quiz.TeacherId != userId)
        {
            return Forbid();
        }

        if (quiz.CategoryId != dto.CategoryId)
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);

            if (category == null)
            {
                return BadRequest(new { message = InvalidCategoryMessage });
            }
        }

        quiz.UpdateQuizFromDto(dto);
        await context.SaveChangesAsync();

        return Ok(quiz.ToQuizResponseDto());
    }

    /// <summary>
    /// Deletes a quiz (soft delete).
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. Only the quiz owner can delete it.</remarks>
    /// <param name="id">The GUID of the quiz to delete.</param>
    /// <response code="204">Quiz deleted successfully.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the quiz.</response>
    /// <response code="404">If the quiz is not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var quiz = await context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null || quiz.IsDeleted)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

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
    /// Publishes or unpublishes a quiz.
    /// </summary>
    /// <remarks>This endpoint requires Teacher role. Only the quiz owner can change its publish status.</remarks>
    /// <param name="id">The GUID of the quiz to publish or unpublish.</param>
    /// <param name="isPublished">Whether to publish (true) or unpublish (false) the quiz.</param>
    /// <returns>The updated quiz details.</returns>
    /// <response code="200">Returns the updated quiz.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Teacher role or does not own the quiz.</response>
    /// <response code="404">If the quiz is not found.</response>
    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = RoleTeacher)]
    [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TogglePublish(Guid id, [FromBody] bool isPublished)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = UserNotAuthenticatedMessage });
        }

        var quiz = await context.Quizzes
            .Include(q => q.Teacher)
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);

        if (quiz == null)
        {
            return NotFound(new { message = QuizNotFoundMessage });
        }

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
