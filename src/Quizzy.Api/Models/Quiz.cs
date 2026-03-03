namespace Quizzy.Api.Models;

public class Quiz : SoftDeleteEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int TimeLimitMinutes { get; set; } // 0 = no time limit
    public int PassingScore { get; set; } // Percentage (e.g., 70 = 70%)
    public int? MaxAttempts { get; set; } // NULL = unlimited attempts
    public bool IsPublished { get; set; }
    public string? AccessCode { get; set; } // NULL = public, non-NULL = invite-only
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}
