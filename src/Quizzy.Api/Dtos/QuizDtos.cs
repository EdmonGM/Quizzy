using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Dtos;

public class QuizResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public int PassingScore { get; set; }
    public int? MaxAttempts { get; set; }
    public bool IsPublished { get; set; }
    public string? AccessCode { get; set; }
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuizDetailResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public int PassingScore { get; set; }
    public int? MaxAttempts { get; set; }
    public bool IsPublished { get; set; }
    public string? AccessCode { get; set; }
    public List<QuestionSummaryDto> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateQuizDto
{
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(512, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, 1440)]
    public int TimeLimitMinutes { get; set; }

    [Range(0, 100)]
    public int PassingScore { get; set; } = 70;

    [Range(1, 100)]
    public int? MaxAttempts { get; set; }

    public bool IsPublished { get; set; } = false;

    [StringLength(64)]
    public string? AccessCode { get; set; }
}

public class UpdateQuizDto
{
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(512, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, 1440)]
    public int TimeLimitMinutes { get; set; }

    [Range(0, 100)]
    public int PassingScore { get; set; } = 70;

    [Range(1, 100)]
    public int? MaxAttempts { get; set; }

    public bool IsPublished { get; set; } = false;

    [StringLength(64)]
    public string? AccessCode { get; set; }
}

public class QuestionSummaryDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Points { get; set; }
}
