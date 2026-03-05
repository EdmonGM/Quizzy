using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Models;

public class StudentAnswer : BaseEntity
{
    public Guid AttemptId { get; set; }
    public QuizAttempt Attempt { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public Guid ChoiceId { get; set; }
    public Choice Choice { get; set; } = null!;
    [MaxLength(128)]
    public string QuestionSnapshot { get; set; } = string.Empty;
    [MaxLength(128)]
    public string ChoiceSnapshot { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
