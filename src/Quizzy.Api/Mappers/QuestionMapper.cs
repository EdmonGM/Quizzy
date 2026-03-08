using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class QuestionMapper
{
    extension(Question question)
    {
        public QuestionResponseDto ToQuestionResponseDto()
        {
            return new QuestionResponseDto
            {
                Id = question.Id,
                QuizId = question.QuizId,
                Content = question.Content,
                OrderIndex = question.OrderIndex,
                Points = question.Points,
                Choices = question.Choices.Select(c => c.ToChoiceResponseDto()).ToList(),
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt
            };
        }

        public QuestionSummaryDto ToQuestionSummaryDto()
        {
            return new QuestionSummaryDto
            {
                Id = question.Id,
                Content =  question.Content,
                OrderIndex = question.OrderIndex,
                Points = question.Points,
            };
        }
    }

    public static Question ToQuestion(this CreateQuestionDto dto)
    {
        return new Question
        {
            Id = Guid.NewGuid(),
            QuizId = dto.QuizId,
            Content = dto.Content,
            OrderIndex = dto.OrderIndex,
            Points = dto.Points,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateQuestionFromDto(this Question question, UpdateQuestionDto dto)
    {
        question.Content = dto.Content;
        question.OrderIndex = dto.OrderIndex;
        question.Points = dto.Points;
        question.UpdatedAt = DateTime.UtcNow;
    }
}
