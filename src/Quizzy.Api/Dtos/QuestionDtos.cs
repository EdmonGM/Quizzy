using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Dtos;

public class QuestionResponseDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Points { get; set; }
    public List<ChoiceResponseDto> Choices { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuestionSummaryDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Points { get; set; }
}

public class CreateQuestionDto
{
    [Required]
    public Guid QuizId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Range(0, 1000)]
    public int OrderIndex { get; set; }

    [Required]
    [Range(1, 100)]
    public int Points { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateChoiceDto> Choices { get; set; } = [];
}

public class UpdateQuestionDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Range(0, 1000)]
    public int OrderIndex { get; set; }

    [Required]
    [Range(1, 100)]
    public int Points { get; set; }

    [Required]
    [MinLength(1)]
    public List<UpdateChoiceDto> Choices { get; set; } = [];
}

public class ReorderQuestionDto
{
    [Required]
    public Guid QuestionId { get; set; }

    [Required]
    [Range(0, 1000)]
    public int OrderIndex { get; set; }
}
