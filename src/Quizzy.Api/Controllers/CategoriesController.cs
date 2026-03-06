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
public class CategoriesController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all not deleted categories
    /// </summary>
    [HttpGet]
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
    /// Get a category by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var category = await context.Categories
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);
        

        if (category == null)
        {
            return NotFound(new { Message = "Category not found" });
        }
        var quizCount = context.Quizzes.Count(q => !q.IsDeleted && q.CategoryId == id);

        return Ok(category.ToCategoryResponseDto(quizCount));
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if category with same name already exists
        var existingCategory = await context.Categories
            .AnyAsync(c => c.Name == dto.Name && !c.IsDeleted);

        if (existingCategory)
        {
            return Conflict(new { Message = "A category with this name already exists" });
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category.ToCategoryResponseDto());
    }

    /// <summary>
    /// Update a category
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var category = await context.Categories
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(new { Message = "Category not found" });
        }

        // Check if new name conflicts with existing category
        if (category.Name != dto.Name)
        {
            var existingCategory = await context.Categories
                .AnyAsync(c => c.Name == dto.Name && c.Id != id && !c.IsDeleted);

            if (existingCategory)
            {
                return Conflict(new { Message = "A category with this name already exists" });
            }
        }

        category.Name = dto.Name;
        category.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(category.ToCategoryResponseDto());
    }

    /// <summary>
    /// Delete a category (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null || category.IsDeleted)
        {
            return NotFound(new { Message = "Category not found" });
        }

        // Check if category has associated quizzes
        var hasQuizzes = await context.Quizzes
            .AnyAsync(q => q.CategoryId == id && !q.IsDeleted);

        if (hasQuizzes)
        {
            return BadRequest(new { Message = "Cannot delete category with associated quizzes" });
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
}
