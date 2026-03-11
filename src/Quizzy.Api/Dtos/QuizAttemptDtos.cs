using System.ComponentModel.DataAnnotations;
using Quizzy.Api.Models;

namespace Quizzy.Api.Dtos;

public class QuizAttemptDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = QuizAttemptStatus.InProgress;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int Score { get; set; }
    public int TotalPossibleScore { get; set; }
    public double Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuizAttemptSummaryDto
{
    public Guid Id { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = QuizAttemptStatus.InProgress;
    public int Score { get; set; }
    public int TotalPossibleScore { get; set; }
    public double Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class QuizAttemptWithAnswersDto : QuizAttemptSummaryDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public int? TimeRemainingSeconds { get; set; }
    public List<QuestionWithUserAnswerDto> Questions { get; set; } = [];
}

public class QuestionWithUserAnswerDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Points { get; set; }
    public Guid? AnswerId { get; set; }
    public List<ChoiceSummaryDto> Choices { get; set; } = [];
}

public class ChoiceSummaryDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class CreateQuizAttemptDto
{
    [Required]
    public Guid QuizId { get; set; }

    [StringLength(64)]
    public string? AccessCode { get; set; }
}

public class SubmitAnswerDto
{
    [Required]
    public Guid QuestionId { get; set; }

    [Required]
    public Guid ChoiceId { get; set; }
}

public class SubmitAttemptResponseDto
{
    public Guid AttemptId { get; set; }
    public string Status { get; set; } = QuizAttemptStatus.Completed;
    public int Score { get; set; }
    public int TotalPossibleScore { get; set; }
    public double Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
    public List<StudentAnswerResultDto> Answers { get; set; } = [];
}

public class QuizAttemptResultsDto : SubmitAttemptResponseDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int PassingScore { get; set; }
    public DateTime StartedAt { get; set; }
    public int TimeLimitMinutes { get; set; }
}

public class QuizAttemptsOverviewDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public int InProgressAttempts { get; set; }
    public int AbandonedAttempts { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double AverageTimeSpentSeconds { get; set; }
    public List<TeacherQuizAttemptDto> Attempts { get; set; } = [];
}

public class TeacherQuizAttemptDto
{
    public Guid Id { get; set; }
    public StudentInfoDto Student { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = QuizAttemptStatus.InProgress;
    public int Score { get; set; }
    public int TotalPossibleScore { get; set; }
    public double Percentage { get; set; }
    public bool Passed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class StudentInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class AbandonAttemptResponseDto
{
    public Guid AttemptId { get; set; }
    public string Status { get; set; } = QuizAttemptStatus.Abandoned;
    public DateTime AbandonedAt { get; set; }
}

public class CreateAttemptResponseDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string Status { get; set; } = QuizAttemptStatus.InProgress;
    public DateTime StartedAt { get; set; }
}

public class SubmitAnswerResponseDto
{
    public Guid AnswerId { get; set; }
    public bool Saved { get; set; }
}
