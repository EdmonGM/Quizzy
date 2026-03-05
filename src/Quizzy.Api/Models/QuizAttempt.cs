using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Models;

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;
    public int AttemptNumber { get; set; }
    [MaxLength(64)]
    public string Status { get; set; } = QuizAttemptStatus.InProgress;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int Score { get; set; }
    public int TotalPossibleScore { get; set; }
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}

public static class QuizAttemptStatus
{
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Abandoned = "Abandoned";
}
