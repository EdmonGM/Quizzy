using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Dtos;

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuizCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCategoryDto
{
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateCategoryDto
{
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
