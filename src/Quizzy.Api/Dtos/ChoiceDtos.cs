using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Dtos;


public class ChoiceResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class CreateChoiceDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public bool IsCorrect { get; set; }

    [Required]
    [Range(0, 100)]
    public int OrderIndex { get; set; }
}

public class UpdateChoiceDto
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public bool IsCorrect { get; set; }

    [Required]
    [Range(0, 100)]
    public int OrderIndex { get; set; }
}