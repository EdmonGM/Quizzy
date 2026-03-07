using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class QuizMapper
{
    public static QuizResponseDto ToQuizResponseDto(this Quiz quiz)
    {
        var questions = quiz.Questions.Select(q => q.ToQuestionSummaryDto()).ToList();
        return new QuizResponseDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            TeacherId = quiz.TeacherId,
            TeacherName = quiz.Teacher.UserName!,
            CategoryId = quiz.CategoryId,
            CategoryName = quiz.Category.Name,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            PassingScore = quiz.PassingScore,
            MaxAttempts = quiz.MaxAttempts,
            IsPublished = quiz.IsPublished,
            AccessCode = quiz.AccessCode,
            Questions = questions,
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    public static QuizDetailResponseDto ToQuizDetailedResponseDto(this Quiz quiz)
    {
        var questions = quiz.Questions.Select(q => q.ToQuestionResponseDto()).ToList();
        return new QuizDetailResponseDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            TeacherId = quiz.TeacherId,
            TeacherName = quiz.Teacher.UserName!,
            CategoryId = quiz.CategoryId,
            CategoryName = quiz.Category.Name,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            PassingScore = quiz.PassingScore,
            MaxAttempts = quiz.MaxAttempts,
            IsPublished = quiz.IsPublished,
            AccessCode = quiz.AccessCode,
            Questions = questions,
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    public static Quiz ToQuiz(this CreateQuizDto dto, string teacherId)
    {
        return new Quiz
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            Title = dto.Title,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            TimeLimitMinutes = dto.TimeLimitMinutes,
            PassingScore = dto.PassingScore,
            MaxAttempts = dto.MaxAttempts,
            IsPublished = dto.IsPublished,
            AccessCode = dto.AccessCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateQuizFromDto(this Quiz quiz, UpdateQuizDto dto)
    {
        quiz.Title = dto.Title;
        quiz.Description = dto.Description;
        quiz.CategoryId = dto.CategoryId;
        quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
        quiz.PassingScore = dto.PassingScore;
        quiz.MaxAttempts = dto.MaxAttempts;
        quiz.IsPublished = dto.IsPublished;
        quiz.AccessCode = dto.AccessCode;
        quiz.UpdatedAt = DateTime.UtcNow;
    }
}
