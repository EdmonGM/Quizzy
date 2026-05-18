using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Constants;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

/// <summary>
/// Manages category operations including listing, retrieving, creating, updating, and deleting categories.
/// </summary>
/// <remarks>
/// This controller requires authentication for all endpoints. Admin role is required for create, update, and delete operations.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController(ApplicationDbContext context) : ControllerBase
{
    private const string CategoryNotFoundMessage = "Category not found";
    private const string DuplicateCategoryNameMessage = "A category with this name already exists";
    private const string CategoryHasQuizzesMessage = "Cannot delete category with associated quizzes";

    /// <summary>
    /// Retrieves all active (non-deleted) categories ordered by name.
    /// </summary>
    /// <returns>List of categories with their quiz counts.</returns>
    /// <response code="200">Returns the list of categories.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await context.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var categoryIds = categories.Select(c => c.Id).ToList();
        var quizCounts = await context.Quizzes
            .Where(q => !q.IsDeleted && categoryIds.Contains(q.CategoryId))
            .GroupBy(q => q.CategoryId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return Ok(categories.Select(c =>
        {
            quizCounts.TryGetValue(c.Id, out var count);
            return c.ToCategoryResponseDto(count);
        }));
    }

    /// <summary>
    /// Retrieves a category by their unique identifier.
    /// </summary>
    /// <param name="id">The GUID of the category to retrieve.</param>
    /// <returns>The category details if found.</returns>
    /// <response code="200">Returns the requested category.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the category is not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var category = await context.Categories
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(new { message = CategoryNotFoundMessage });
        }

        var quizCount = await context.Quizzes
            .CountAsync(q => !q.IsDeleted && q.CategoryId == id);

        return Ok(category.ToCategoryResponseDto(quizCount));
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <remarks>This endpoint requires Admin role.</remarks>
    /// <param name="dto">The category data to create.</param>
    /// <returns>The created category details.</returns>
    /// <response code="201">Returns the newly created category.</response>
    /// <response code="400">If the request body is invalid.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Admin role.</response>
    /// <response code="409">If a category with the same name already exists.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var existingCategory = await context.Categories
            .AnyAsync(c => c.Name == dto.Name && !c.IsDeleted);

        if (existingCategory)
        {
            return Conflict(new { message = DuplicateCategoryNameMessage });
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category.ToCategoryResponseDto());
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <remarks>This endpoint requires Admin role.</remarks>
    /// <param name="id">The GUID of the category to update.</param>
    /// <param name="dto">The updated category data.</param>
    /// <returns>The updated category details.</returns>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="400">If the request body is invalid.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Admin role.</response>
    /// <response code="404">If the category is not found.</response>
    /// <response code="409">If a category with the same name already exists.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await context.Categories
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(new { message = CategoryNotFoundMessage });
        }

        if (category.Name != dto.Name)
        {
            var existingCategory = await context.Categories
                .AnyAsync(c => c.Name == dto.Name && c.Id != id && !c.IsDeleted);

            if (existingCategory)
            {
                return Conflict(new { message = DuplicateCategoryNameMessage });
            }
        }

        category.Name = dto.Name;
        category.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var quizCount = await context.Quizzes
            .CountAsync(q => !q.IsDeleted && q.CategoryId == id);

        return Ok(category.ToCategoryResponseDto(quizCount));
    }

    /// <summary>
    /// Deletes a category (soft delete).
    /// </summary>
    /// <remarks>This endpoint requires Admin role. Category cannot be deleted if it has associated quizzes.</remarks>
    /// <param name="id">The GUID of the category to delete.</param>
    /// <response code="204">Category deleted successfully.</response>
    /// <response code="400">If the category has associated quizzes.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Admin role.</response>
    /// <response code="404">If the category is not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null || category.IsDeleted)
        {
            return NotFound(new { message = CategoryNotFoundMessage });
        }

        var hasQuizzes = await context.Quizzes
            .AnyAsync(q => q.CategoryId == id && !q.IsDeleted);

        if (hasQuizzes)
        {
            return BadRequest(new { message = CategoryHasQuizzesMessage });
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
}
