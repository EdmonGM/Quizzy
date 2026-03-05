using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class QuizMapper
{
    public static async Task<QuizResponseDto> ToQuizResponseDtoAsync(this Quiz quiz, ApplicationDbContext context)
    {
        var questionCount = await context.Questions
            .CountAsync(q => q.QuizId == quiz.Id);

        var teacherName = await context.Users
            .Where(u => u.Id == quiz.TeacherId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return new QuizResponseDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            TeacherId = quiz.TeacherId,
            TeacherName = teacherName,
            CategoryId = quiz.CategoryId,
            CategoryName = quiz.Category.Name,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            PassingScore = quiz.PassingScore,
            MaxAttempts = quiz.MaxAttempts,
            IsPublished = quiz.IsPublished,
            AccessCode = quiz.AccessCode,
            QuestionCount = questionCount,
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    public static async Task<QuizDetailResponseDto> ToQuizDetailResponseDtoAsync(this Quiz quiz, ApplicationDbContext context)
    {
        var teacherName = await context.Users
            .Where(u => u.Id == quiz.TeacherId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync() ?? string.Empty;

        var questions = await context.Questions
            .Where(q => q.QuizId == quiz.Id)
            .OrderBy(q => q.OrderIndex)
            .Select(q => new QuestionSummaryDto
            {
                Id = q.Id,
                Content = q.Content,
                OrderIndex = q.OrderIndex,
                Points = q.Points
            })
            .ToListAsync();

        return new QuizDetailResponseDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            TeacherId = quiz.TeacherId,
            TeacherName = teacherName,
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

    public static Quiz ToQuiz(this CreateQuizDto dto)
    {
        return new Quiz
        {
            Id = Guid.NewGuid(),
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
